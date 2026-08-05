using CertMaster.Application.Common.Interfaces;
using CertMaster.Application.Features.Notifications;
using CertMaster.Domain.Entities;
using CertMaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Billing;

public record UpgradeRequest(string Plan); // "PremiumMonthly" or "PremiumYearly"

public record UpgradeResultDto(bool Succeeded, string Plan, decimal AmountZar, string? FailureReason);

public class BillingService
{
    private static readonly Dictionary<SubscriptionPlan, decimal> PlanPricingZar = new()
    {
        [SubscriptionPlan.PremiumMonthly] = 40m,
        [SubscriptionPlan.PremiumYearly] = 450m,
    };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentGatewayProvider _paymentGateway;
    private readonly IDateTimeProvider _clock;

    public BillingService(
        IApplicationDbContext db, ICurrentUserService currentUser, IPaymentGatewayProvider paymentGateway, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
        _clock = clock;
    }

    public async Task<UpgradeResultDto> UpgradeAsync(UpgradeRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new UnauthorizedAccessException();

        if (!Enum.TryParse<SubscriptionPlan>(request.Plan, out var plan) || plan == SubscriptionPlan.Free)
            throw new InvalidOperationException("Invalid plan. Choose PremiumMonthly or PremiumYearly.");

        var user = await _db.Users.FirstAsync(u => u.Id == _currentUser.UserId, ct);
        var amount = PlanPricingZar[plan];

        var chargeResult = await _paymentGateway.ChargeAsync(user.Id, user.Email, plan, amount, ct);

        var transaction = new PaymentTransaction
        {
            UserId = user.Id,
            User = user,
            Plan = plan,
            AmountZar = amount,
            Status = chargeResult.Succeeded ? "Paid" : "Failed",
            ProviderReference = chargeResult.ProviderReference,
        };
        _db.PaymentTransactions.Add(transaction);

        if (chargeResult.Succeeded)
        {
            user.Plan = plan;
            user.PremiumExpiresAtUtc = plan == SubscriptionPlan.PremiumYearly
                ? _clock.UtcNow.AddYears(1)
                : _clock.UtcNow.AddMonths(1);

            _db.Notifications.Add(NotificationService.Build(
                user.Id,
                "Upgrade successful",
                $"You're now on the {(plan == SubscriptionPlan.PremiumYearly ? "yearly" : "monthly")} Premium plan. Enjoy unlimited practice and mock exams!",
                "Billing"));
        }
        else
        {
            _db.Notifications.Add(NotificationService.Build(
                user.Id,
                "Payment failed",
                chargeResult.FailureReason ?? "Your payment could not be processed. Please try again.",
                "Billing"));
        }

        await _db.SaveChangesAsync(ct);

        return new UpgradeResultDto(chargeResult.Succeeded, plan.ToString(), amount, chargeResult.FailureReason);
    }
}

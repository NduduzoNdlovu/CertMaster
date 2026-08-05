using CertMaster.Application.Common.Interfaces;
using CertMaster.Domain.Enums;

namespace CertMaster.Infrastructure.Payments;

/// <summary>
/// Development/demo payment provider. Always "succeeds" and generates a fake
/// provider reference — no real money moves and no real gateway is contacted.
/// To integrate a real provider (Stripe, PayFast, Paystack, etc.), implement
/// IPaymentGatewayProvider against that provider's SDK/API and register it
/// instead of this class in Infrastructure's DependencyInjection.
/// </summary>
public class MockPaymentGatewayProvider : IPaymentGatewayProvider
{
    public Task<PaymentInitiationResult> ChargeAsync(
        Guid userId, string userEmail, SubscriptionPlan plan, decimal amountZar, CancellationToken ct)
    {
        var reference = $"MOCK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
        return Task.FromResult(new PaymentInitiationResult(
            Succeeded: true,
            ProviderReference: reference,
            RedirectUrl: null,
            FailureReason: null));
    }
}

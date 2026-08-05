using CertMaster.Domain.Enums;

namespace CertMaster.Application.Common.Interfaces;

public record PaymentInitiationResult(bool Succeeded, string? ProviderReference, string? RedirectUrl, string? FailureReason);

/// <summary>
/// Abstraction over a payment gateway (Stripe, PayFast, Paystack, etc). Swap the
/// registered implementation in Infrastructure's DependencyInjection to integrate
/// a real provider — nothing in the Application or Api layers needs to change.
/// </summary>
public interface IPaymentGatewayProvider
{
    Task<PaymentInitiationResult> ChargeAsync(Guid userId, string userEmail, SubscriptionPlan plan, decimal amountZar, CancellationToken ct);
}

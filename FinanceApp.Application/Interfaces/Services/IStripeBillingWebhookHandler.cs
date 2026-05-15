namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Applies Stripe webhook events to shared subscription entitlements.
/// </summary>
public interface IStripeBillingWebhookHandler
{
    Task ProcessWebhookAsync(string jsonBody, string? stripeSignatureHeader, CancellationToken cancellationToken = default);
}

namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Verifies a Play Billing subscription with Google Play Developer API.
/// </summary>
public interface IGooglePlaySubscriptionVerifier
{
    Task<GoogleVerifiedSubscription?> VerifySubscriptionAsync(
        string subscriptionId,
        string purchaseToken,
        CancellationToken cancellationToken = default);
}

public sealed record GoogleVerifiedSubscription(
    string OrderId,
    string ProductId,
    DateTimeOffset ExpiresAtUtc,
    bool AutoRenewing);

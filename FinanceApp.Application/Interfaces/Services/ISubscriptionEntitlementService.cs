using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Central place to apply subscription entitlements after verified store purchase or admin action.
/// </summary>
public interface ISubscriptionEntitlementService
{
    /// <summary>
    /// Applies a verified subscription grant.
    /// </summary>
    /// <param name="googlePurchaseToken">Play Billing token to store on the user for revalidation (Google only).</param>
    Task<SubscriptionEntitlementResult> ApplyVerifiedEntitlementAsync(
        string userId,
        SubscriptionPlan plan,
        DateTimeOffset expiresAtUtc,
        SubscriptionBillingSource source,
        string externalTransactionId,
        string productId,
        string? notes,
        string? googlePurchaseToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// If <see cref="SubscriptionExpiresAtUtc"/> is in the past, downgrade user to Free and clear store tokens.
    /// </summary>
    Task SyncExpiredSubscriptionAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Finds a user previously linked via verify/apple using the Apple original transaction id.</summary>
    Task<string?> FindUserIdByAppleOriginalTransactionIdAsync(
        string originalTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds a user previously linked via verify/google using the Play purchase token.</summary>
    Task<string?> FindUserIdByGooglePurchaseTokenAsync(
        string purchaseToken,
        CancellationToken cancellationToken = default);

    /// <summary>Finds a user linked to Stripe web billing via customer id.</summary>
    Task<string?> FindUserIdByStripeCustomerIdAsync(
        string stripeCustomerId,
        CancellationToken cancellationToken = default);

    /// <summary>Downgrades a store-billed user to Free (refund, expiry, revoke).</summary>
    Task RevokeStoreSubscriptionAsync(
        string userId,
        string? notes,
        CancellationToken cancellationToken = default);
}

public sealed record SubscriptionEntitlementResult(bool Success, string? ErrorMessage);

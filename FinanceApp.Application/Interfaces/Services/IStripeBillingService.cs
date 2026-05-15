using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Stripe Checkout and Customer Portal for web subscribers.
/// </summary>
public interface IStripeBillingService
{
    bool IsConfigured { get; }

    /// <summary>Creates a hosted Checkout session URL for the given plan, or null when Stripe is not configured.</summary>
    Task<string?> CreateCheckoutSessionUrlAsync(
        string userId,
        string email,
        SubscriptionPlan plan,
        string successUrl,
        string cancelUrl,
        string? existingStripeCustomerId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a Customer Portal session URL, or null when the user has no Stripe customer.</summary>
    Task<string?> CreateCustomerPortalUrlAsync(
        string stripeCustomerId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    /// <summary>Loads subscription period end and price id from Stripe (used after checkout).</summary>
    Task<StripeSubscriptionSnapshot?> GetSubscriptionSnapshotAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default);
}

public sealed record StripeSubscriptionSnapshot(
    string SubscriptionId,
    string CustomerId,
    string PriceId,
    DateTimeOffset CurrentPeriodEndUtc,
    bool IsActiveOrTrialing);

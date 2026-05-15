using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.BillingPortal;
using Stripe.Checkout;

namespace FinanceApp.Infrastructure.Subscription;

public sealed class StripeBillingService : IStripeBillingService
{
    private readonly string? _secretKey;
    private readonly SubscriptionProductMapper _productMapper;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(
        IConfiguration config,
        SubscriptionProductMapper productMapper,
        ILogger<StripeBillingService> logger)
    {
        _secretKey = config["SubscriptionBilling:Stripe:SecretKey"];
        _productMapper = productMapper;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_secretKey);

    public async Task<string?> CreateCheckoutSessionUrlAsync(
        string userId,
        string email,
        SubscriptionPlan plan,
        string successUrl,
        string cancelUrl,
        string? existingStripeCustomerId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || plan == SubscriptionPlan.Free)
            return null;

        var priceId = _productMapper.GetStripePriceId(plan);
        if (priceId == null)
        {
            _logger.LogWarning("No Stripe price configured for plan {Plan}.", plan);
            return null;
        }

        StripeConfiguration.ApiKey = _secretKey;

        var options = new Stripe.Checkout.SessionCreateOptions
        {
            Mode = "subscription",
            ClientReferenceId = userId,
            LineItems =
            [
                new Stripe.Checkout.SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1
                }
            ],
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId,
                ["plan"] = plan.ToString()
            }
        };

        if (!string.IsNullOrWhiteSpace(existingStripeCustomerId))
            options.Customer = existingStripeCustomerId;
        else
            options.CustomerEmail = email;

        var session = await new Stripe.Checkout.SessionService().CreateAsync(options, cancellationToken: cancellationToken);
        return session.Url;
    }

    public async Task<string?> CreateCustomerPortalUrlAsync(
        string stripeCustomerId,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(stripeCustomerId))
            return null;

        StripeConfiguration.ApiKey = _secretKey;

        var session = await new Stripe.BillingPortal.SessionService().CreateAsync(
            new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = stripeCustomerId,
                ReturnUrl = returnUrl
            },
            cancellationToken: cancellationToken);

        return session.Url;
    }

    public async Task<StripeSubscriptionSnapshot?> GetSubscriptionSnapshotAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(stripeSubscriptionId))
            return null;

        StripeConfiguration.ApiKey = _secretKey;

        var sub = await new global::Stripe.SubscriptionService().GetAsync(stripeSubscriptionId, cancellationToken: cancellationToken);
        var item = sub.Items?.Data?.FirstOrDefault();
        var priceId = item?.Price?.Id;
        if (priceId == null || item?.CurrentPeriodEnd == null)
            return null;

        var periodEnd = new DateTimeOffset(item.CurrentPeriodEnd, TimeSpan.Zero);
        var active = sub.Status is "active" or "trialing";

        return new StripeSubscriptionSnapshot(
            sub.Id,
            sub.CustomerId,
            priceId,
            periodEnd,
            active);
    }
}

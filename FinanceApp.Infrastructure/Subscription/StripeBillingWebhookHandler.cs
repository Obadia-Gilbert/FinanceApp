using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace FinanceApp.Infrastructure.Subscription;

public sealed class StripeBillingWebhookHandler : IStripeBillingWebhookHandler
{
    private readonly ISubscriptionEntitlementService _entitlement;
    private readonly IStripeBillingService _stripeBilling;
    private readonly SubscriptionProductMapper _productMapper;
    private readonly IRepository<SubscriptionPurchaseRecord> _purchaseRecords;
    private readonly string? _webhookSecret;
    private readonly ILogger<StripeBillingWebhookHandler> _logger;

    public StripeBillingWebhookHandler(
        ISubscriptionEntitlementService entitlement,
        IStripeBillingService stripeBilling,
        SubscriptionProductMapper productMapper,
        IRepository<SubscriptionPurchaseRecord> purchaseRecords,
        IConfiguration config,
        ILogger<StripeBillingWebhookHandler> logger)
    {
        _entitlement = entitlement;
        _stripeBilling = stripeBilling;
        _productMapper = productMapper;
        _purchaseRecords = purchaseRecords;
        _webhookSecret = config["SubscriptionBilling:Stripe:WebhookSecret"];
        _logger = logger;
    }

    public async Task ProcessWebhookAsync(
        string jsonBody,
        string? stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        Event stripeEvent;
        try
        {
            if (!string.IsNullOrWhiteSpace(_webhookSecret))
            {
                if (string.IsNullOrWhiteSpace(stripeSignatureHeader))
                {
                    _logger.LogWarning("Stripe webhook missing signature header.");
                    return;
                }

                stripeEvent = EventUtility.ConstructEvent(jsonBody, stripeSignatureHeader, _webhookSecret);
            }
            else
            {
                stripeEvent = EventUtility.ParseEvent(jsonBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature or payload invalid.");
            return;
        }

        if (!string.IsNullOrEmpty(stripeEvent.Id) &&
            await IsDuplicateWebhookAsync($"stripe:{stripeEvent.Id}", cancellationToken))
        {
            _logger.LogInformation("Stripe webhook duplicate ignored: {Id}", stripeEvent.Id);
            return;
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.updated":
            case "customer.subscription.created":
                await HandleSubscriptionChangedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken);
                break;
        }

        await RecordWebhookAuditAsync(stripeEvent.Id, stripeEvent.Type, cancellationToken);
    }

    private async Task HandleCheckoutCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = TryGetCheckoutSession(stripeEvent);
        if (session == null)
        {
            _logger.LogWarning(
                "Stripe checkout webhook could not parse Session (object type: {ActualType}).",
                stripeEvent.Data.Object?.GetType().Name ?? "null");
            return;
        }

        var userId = session.ClientReferenceId
            ?? (session.Metadata != null && session.Metadata.TryGetValue("userId", out var uid) ? uid : null);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            _logger.LogWarning("Stripe checkout.session.completed missing user or subscription id.");
            return;
        }

        var snapshot = await _stripeBilling.GetSubscriptionSnapshotAsync(session.SubscriptionId, cancellationToken);
        if (snapshot == null || !snapshot.IsActiveOrTrialing)
            return;

        if (!_productMapper.TryMapStripe(snapshot.PriceId, out var plan))
        {
            _logger.LogWarning("Stripe checkout unknown price id {PriceId}.", snapshot.PriceId);
            return;
        }

        await _entitlement.ApplyVerifiedEntitlementAsync(
            userId,
            plan,
            snapshot.CurrentPeriodEndUtc,
            SubscriptionBillingSource.Web,
            snapshot.SubscriptionId,
            snapshot.PriceId,
            notes: "webhook:stripe:checkout.session.completed",
            googlePurchaseToken: snapshot.CustomerId,
            cancellationToken);
    }

    private async Task HandleSubscriptionChangedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscription = TryGetSubscription(stripeEvent);
        if (subscription == null)
        {
            _logger.LogWarning(
                "Stripe webhook {Type}: could not parse Subscription (object type: {ActualType}).",
                stripeEvent.Type,
                stripeEvent.Data.Object?.GetType().Name ?? "null");
            return;
        }

        var userId = await _entitlement.FindUserIdByStripeCustomerIdAsync(subscription.CustomerId, cancellationToken);
        if (userId == null)
        {
            _logger.LogWarning("Stripe subscription update: no user for customer {CustomerId}.", subscription.CustomerId);
            return;
        }

        if (subscription.Status is "canceled" or "unpaid" or "incomplete_expired")
        {
            await _entitlement.RevokeStoreSubscriptionAsync(userId, $"webhook:stripe:{stripeEvent.Type}:{subscription.Status}", cancellationToken);
            return;
        }

        if (subscription.Status is not ("active" or "trialing"))
            return;

        var item = subscription.Items?.Data?.FirstOrDefault();
        var priceId = item?.Price?.Id;
        if (priceId == null || !_productMapper.TryMapStripe(priceId, out var plan))
        {
            _logger.LogWarning("Stripe subscription update unknown price {PriceId}.", priceId);
            return;
        }

        var periodEndRaw = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
        DateTimeOffset periodEnd;
        if (periodEndRaw != null)
            periodEnd = new DateTimeOffset(periodEndRaw.Value, TimeSpan.Zero);
        else if (subscription.Status is "active" or "trialing")
            periodEnd = DateTimeOffset.UtcNow.AddDays(31);
        else
            return;

        await _entitlement.ApplyVerifiedEntitlementAsync(
            userId,
            plan,
            periodEnd,
            SubscriptionBillingSource.Web,
            subscription.Id,
            priceId,
            notes: $"webhook:stripe:{stripeEvent.Type}",
            googlePurchaseToken: subscription.CustomerId,
            cancellationToken);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscription = TryGetSubscription(stripeEvent);
        if (subscription == null)
            return;

        var userId = await _entitlement.FindUserIdByStripeCustomerIdAsync(subscription.CustomerId, cancellationToken);
        if (userId == null)
            return;

        await _entitlement.RevokeStoreSubscriptionAsync(
            userId,
            $"webhook:stripe:{stripeEvent.Type}",
            cancellationToken);
    }

    private static Stripe.Checkout.Session? TryGetCheckoutSession(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is Stripe.Checkout.Session session)
            return session;

        if (stripeEvent.Data.RawObject == null)
            return null;

        try
        {
            return StripeEntity.FromJson<Stripe.Checkout.Session>(stripeEvent.Data.RawObject.ToString()!);
        }
        catch
        {
            return null;
        }
    }

    private static global::Stripe.Subscription? TryGetSubscription(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is global::Stripe.Subscription subscription)
            return subscription;

        if (stripeEvent.Data.RawObject == null)
            return null;

        try
        {
            return StripeEntity.FromJson<global::Stripe.Subscription>(stripeEvent.Data.RawObject.ToString()!);
        }
        catch
        {
            return null;
        }
    }

    private static DateTimeOffset ResolvePeriodEndUtc(global::Stripe.Subscription subscription)
    {
        var itemEnd = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
        if (itemEnd.HasValue)
            return new DateTimeOffset(DateTime.SpecifyKind(itemEnd.Value, DateTimeKind.Utc));

        return DateTimeOffset.UtcNow.AddDays(30);
    }

    private async Task<bool> IsDuplicateWebhookAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var note = $"webhook-id:{idempotencyKey}";
        var existing = await _purchaseRecords.FindAsync(r => r.Notes == note);
        return existing.Any();
    }

    private async Task RecordWebhookAuditAsync(string? eventId, string? eventType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(eventId))
            return;

        await _purchaseRecords.AddAsync(new SubscriptionPurchaseRecord
        {
            UserId = "webhook",
            BillingSource = SubscriptionBillingSource.Web,
            ProductId = eventType ?? "unknown",
            ExternalTransactionId = eventId,
            Plan = SubscriptionPlan.Free,
            ExpiresAtUtc = null,
            Notes = $"webhook-id:stripe:{eventId}"
        });
        await _purchaseRecords.SaveChangesAsync();
    }
}

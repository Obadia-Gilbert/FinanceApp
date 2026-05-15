using System.Text;
using System.Text.Json;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Subscription;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Infrastructure.Services;

public sealed class SubscriptionBillingWebhookService : ISubscriptionBillingWebhookService
{
    private readonly IAppleStoreTransactionVerifier _appleVerifier;
    private readonly IGooglePlaySubscriptionVerifier _googleVerifier;
    private readonly ISubscriptionEntitlementService _entitlement;
    private readonly SubscriptionProductMapper _productMapper;
    private readonly IRepository<SubscriptionPurchaseRecord> _purchaseRecords;
    private readonly IConfiguration _config;
    private readonly ILogger<SubscriptionBillingWebhookService> _logger;

    public SubscriptionBillingWebhookService(
        IAppleStoreTransactionVerifier appleVerifier,
        IGooglePlaySubscriptionVerifier googleVerifier,
        ISubscriptionEntitlementService entitlement,
        SubscriptionProductMapper productMapper,
        IRepository<SubscriptionPurchaseRecord> purchaseRecords,
        IConfiguration config,
        ILogger<SubscriptionBillingWebhookService> logger)
    {
        _appleVerifier = appleVerifier;
        _googleVerifier = googleVerifier;
        _entitlement = entitlement;
        _productMapper = productMapper;
        _purchaseRecords = purchaseRecords;
        _config = config;
        _logger = logger;
    }

    public async Task ProcessAppleNotificationAsync(JsonElement body, CancellationToken cancellationToken = default)
    {
        if (!body.TryGetProperty("signedPayload", out var signedPayloadEl))
        {
            _logger.LogWarning("Apple webhook missing signedPayload.");
            return;
        }

        var signedPayload = signedPayloadEl.GetString();
        if (string.IsNullOrWhiteSpace(signedPayload))
            return;

        if (!AppleJwsVerifier.TryReadVerifiedToken(signedPayload, _config, _logger, out var notificationJwt))
            return;

        using var notificationDoc = JsonDocument.Parse(notificationJwt.Payload.SerializeToJson());
        var root = notificationDoc.RootElement;

        var notificationType = root.TryGetProperty("notificationType", out var nt) ? nt.GetString() : null;
        var subtype = root.TryGetProperty("subtype", out var st) ? st.GetString() : null;
        var notificationUuid = root.TryGetProperty("notificationUUID", out var nu) ? nu.GetString() : null;

        if (!string.IsNullOrEmpty(notificationUuid) &&
            await IsDuplicateWebhookAsync($"apple:{notificationUuid}", cancellationToken))
        {
            _logger.LogInformation("Apple webhook duplicate ignored: {Uuid}", notificationUuid);
            return;
        }

        if (!root.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("signedTransactionInfo", out var signedTxEl))
        {
            _logger.LogInformation("Apple webhook {Type}/{Subtype} has no signedTransactionInfo.", notificationType, subtype);
            await RecordWebhookAuditAsync("apple", notificationUuid, notificationType, null, cancellationToken);
            return;
        }

        var signedTransaction = signedTxEl.GetString();
        if (string.IsNullOrWhiteSpace(signedTransaction))
            return;

        var verified = await _appleVerifier.VerifySignedTransactionAsync(signedTransaction, cancellationToken);
        if (verified == null)
        {
            _logger.LogWarning("Apple webhook could not verify signedTransactionInfo.");
            return;
        }

        var userId = await _entitlement.FindUserIdByAppleOriginalTransactionIdAsync(
            verified.OriginalTransactionId,
            cancellationToken);

        if (userId == null)
        {
            _logger.LogWarning(
                "Apple webhook {Type}: no user linked to originalTransactionId {Tx}. Client verify/apple must run first.",
                notificationType,
                verified.OriginalTransactionId);
            await RecordWebhookAuditAsync("apple", notificationUuid, notificationType, verified.OriginalTransactionId, cancellationToken);
            return;
        }

        if (IsAppleRevocation(notificationType, subtype))
        {
            await _entitlement.RevokeStoreSubscriptionAsync(
                userId,
                $"webhook:apple:{notificationType}:{subtype}",
                cancellationToken);
        }
        else if (IsAppleActivation(notificationType))
        {
            if (!_productMapper.TryMapApple(verified.ProductId, out var plan))
            {
                _logger.LogWarning("Apple webhook unknown product id {ProductId}.", verified.ProductId);
                return;
            }

            await _entitlement.ApplyVerifiedEntitlementAsync(
                userId,
                plan,
                verified.ExpiresAtUtc,
                SubscriptionBillingSource.Apple,
                verified.OriginalTransactionId,
                verified.ProductId,
                notes: $"webhook:apple:{notificationType}",
                googlePurchaseToken: null,
                cancellationToken);
        }

        await RecordWebhookAuditAsync("apple", notificationUuid, notificationType, verified.OriginalTransactionId, cancellationToken);
    }

    public async Task ProcessGoogleNotificationAsync(JsonElement body, CancellationToken cancellationToken = default)
    {
        var inner = TryUnwrapGooglePubSub(body);
        if (inner == null)
        {
            _logger.LogWarning("Google webhook payload could not be parsed.");
            return;
        }

        if (!inner.Value.TryGetProperty("subscriptionNotification", out var subNotif))
        {
            _logger.LogInformation("Google webhook without subscriptionNotification (ignored).");
            return;
        }

        var purchaseToken = subNotif.TryGetProperty("purchaseToken", out var pt) ? pt.GetString() : null;
        var subscriptionId = subNotif.TryGetProperty("subscriptionId", out var sid) ? sid.GetString() : null;
        var notificationType = subNotif.TryGetProperty("notificationType", out var ntt) ? ntt.GetInt32() : 0;

        if (string.IsNullOrWhiteSpace(purchaseToken) || string.IsNullOrWhiteSpace(subscriptionId))
            return;

        var messageId = body.TryGetProperty("message", out var msg) && msg.TryGetProperty("messageId", out var mid)
            ? mid.GetString()
            : null;

        if (!string.IsNullOrEmpty(messageId) &&
            await IsDuplicateWebhookAsync($"google:{messageId}", cancellationToken))
        {
            _logger.LogInformation("Google webhook duplicate ignored: {MessageId}", messageId);
            return;
        }

        var userId = await _entitlement.FindUserIdByGooglePurchaseTokenAsync(purchaseToken, cancellationToken);

        // 12 = REVOKED, 13 = EXPIRED
        if (notificationType is 12 or 13)
        {
            if (userId != null)
            {
                await _entitlement.RevokeStoreSubscriptionAsync(
                    userId,
                    $"webhook:google:{notificationType}",
                    cancellationToken);
            }

            await RecordWebhookAuditAsync("google", messageId, notificationType.ToString(), purchaseToken, cancellationToken);
            return;
        }

        var verified = await _googleVerifier.VerifySubscriptionAsync(subscriptionId, purchaseToken, cancellationToken);
        if (verified == null)
        {
            _logger.LogWarning("Google webhook re-verification failed for {SubscriptionId}.", subscriptionId);
            return;
        }

        if (userId == null)
        {
            _logger.LogWarning(
                "Google webhook type {Type}: no user for purchase token. Client verify/google must run first.",
                notificationType);
            await RecordWebhookAuditAsync("google", messageId, notificationType.ToString(), purchaseToken, cancellationToken);
            return;
        }

        if (!_productMapper.TryMapGoogle(verified.ProductId, out var plan))
        {
            _logger.LogWarning("Google webhook unknown subscription id {Id}.", verified.ProductId);
            return;
        }

        await _entitlement.ApplyVerifiedEntitlementAsync(
            userId,
            plan,
            verified.ExpiresAtUtc,
            SubscriptionBillingSource.Google,
            verified.OrderId,
            verified.ProductId,
            notes: $"webhook:google:{notificationType}",
            googlePurchaseToken: purchaseToken,
            cancellationToken);

        await RecordWebhookAuditAsync("google", messageId, notificationType.ToString(), purchaseToken, cancellationToken);
    }

    private static JsonElement? TryUnwrapGooglePubSub(JsonElement body)
    {
        if (body.TryGetProperty("message", out var message) &&
            message.TryGetProperty("data", out var dataEl))
        {
            var b64 = dataEl.GetString();
            if (string.IsNullOrWhiteSpace(b64))
                return null;

            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }
            catch
            {
                return null;
            }
        }

        return body.Clone();
    }

    private static bool IsAppleActivation(string? notificationType) =>
        notificationType is "SUBSCRIBED" or "DID_RENEW" or "DID_CHANGE_RENEWAL_PREF" or "OFFER_REDEEMED";

    private static bool IsAppleRevocation(string? notificationType, string? subtype) =>
        notificationType is "EXPIRED" or "GRACE_PERIOD_EXPIRED" or "REFUND" or "REVOKE" or "DID_FAIL_TO_RENEW"
        || (notificationType == "DID_CHANGE_RENEWAL_STATUS" && subtype == "AUTO_RENEW_DISABLED");

    private async Task<bool> IsDuplicateWebhookAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var note = $"webhook-id:{idempotencyKey}";
        var existing = await _purchaseRecords.FindAsync(r => r.Notes == note);
        return existing.Any();
    }

    private async Task RecordWebhookAuditAsync(
        string store,
        string? eventId,
        string? eventType,
        string? externalId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(eventId))
            return;

        await _purchaseRecords.AddAsync(new SubscriptionPurchaseRecord
        {
            UserId = "webhook",
            BillingSource = store == "apple" ? SubscriptionBillingSource.Apple : SubscriptionBillingSource.Google,
            ProductId = eventType ?? "unknown",
            ExternalTransactionId = externalId ?? eventId,
            Plan = SubscriptionPlan.Free,
            ExpiresAtUtc = null,
            Notes = $"webhook-id:{store}:{eventId}"
        });
        await _purchaseRecords.SaveChangesAsync();
    }
}

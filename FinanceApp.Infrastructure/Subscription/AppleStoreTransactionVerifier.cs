using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using FinanceApp.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Infrastructure.Subscription;

/// <summary>
/// Verifies StoreKit 2 signed transaction JWS using the leaf certificate in the x5c header (ES256).
/// Configure <c>SubscriptionBilling:Apple:AllowUnsignedPayloadInDevelopment</c> only for local mock testing — never in production.
/// </summary>
public sealed class AppleStoreTransactionVerifier : IAppleStoreTransactionVerifier
{
    private readonly IConfiguration _config;
    private readonly ILogger<AppleStoreTransactionVerifier> _logger;

    public AppleStoreTransactionVerifier(
        IConfiguration config,
        ILogger<AppleStoreTransactionVerifier> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<AppleVerifiedTransaction?> VerifySignedTransactionAsync(string signedTransactionJws, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signedTransactionJws))
            return Task.FromResult<AppleVerifiedTransaction?>(null);

        try
        {
            if (!AppleJwsVerifier.TryReadVerifiedToken(signedTransactionJws, _config, _logger, out var jwt))
                return Task.FromResult<AppleVerifiedTransaction?>(null);

            var payloadJson = jwt.Payload.SerializeToJson();
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            var originalTx = root.TryGetProperty("originalTransactionId", out var o) ? o.GetString() : null;
            var productId = root.TryGetProperty("productId", out var p) ? p.GetString() : null;
            if (string.IsNullOrEmpty(originalTx) || string.IsNullOrEmpty(productId))
                return Task.FromResult<AppleVerifiedTransaction?>(null);

            DateTimeOffset expires;
            if (root.TryGetProperty("expiresDate", out var ed) && ed.ValueKind == JsonValueKind.Number)
                expires = DateTimeOffset.FromUnixTimeMilliseconds(ed.GetInt64());
            else if (root.TryGetProperty("expiresDate", out ed) && ed.ValueKind == JsonValueKind.String
                     && long.TryParse(ed.GetString(), out var ms))
                expires = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            else
            {
                _logger.LogWarning("Apple transaction payload missing expiresDate.");
                return Task.FromResult<AppleVerifiedTransaction?>(null);
            }

            var autoRenew = root.TryGetProperty("autoRenewStatus", out var ar) && ar.ValueKind == JsonValueKind.Number
                ? ar.GetInt32() == 1
                : true;

            return Task.FromResult<AppleVerifiedTransaction?>(new AppleVerifiedTransaction(
                originalTx,
                productId,
                expires,
                autoRenew));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify Apple StoreKit JWS.");
            return Task.FromResult<AppleVerifiedTransaction?>(null);
        }
    }
}

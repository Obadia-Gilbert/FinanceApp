using System.Collections;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using FinanceApp.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

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

        var allowUnsafe = _config.GetValue("SubscriptionBilling:Apple:AllowUnsignedPayloadInDevelopment", false);

        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            JwtSecurityToken jwt;
            if (allowUnsafe)
            {
                jwt = handler.ReadJwtToken(signedTransactionJws);
                _logger.LogWarning("Apple transaction JWS accepted without signature verification (AllowUnsignedPayloadInDevelopment).");
            }
            else
            {
                var token = handler.ReadJwtToken(signedTransactionJws);
                if (!TryGetX5cLeaf(token, out var leafPem))
                {
                    _logger.LogWarning("Apple JWS missing x5c header.");
                    return Task.FromResult<AppleVerifiedTransaction?>(null);
                }

                var certBytes = Convert.FromBase64String(leafPem);
                using var leaf = X509CertificateLoader.LoadCertificate(certBytes);
                using var ecdsa = leaf.GetECDsaPublicKey();
                if (ecdsa == null)
                {
                    _logger.LogWarning("Apple leaf certificate has no ECDsa public key.");
                    return Task.FromResult<AppleVerifiedTransaction?>(null);
                }

                var key = new ECDsaSecurityKey(ecdsa);
                var validation = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(10)
                };

                handler.ValidateToken(signedTransactionJws, validation, out _);
                jwt = token;
            }

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

    private static bool TryGetX5cLeaf(JwtSecurityToken token, out string leaf)
    {
        leaf = null!;
        if (!token.Header.TryGetValue("x5c", out var x5cObj) || x5cObj == null)
            return false;

        if (x5cObj is IList list && list.Count > 0 && list[0] is string s)
        {
            leaf = s;
            return true;
        }

        if (x5cObj is string[] arr && arr.Length > 0)
        {
            leaf = arr[0];
            return true;
        }

        return false;
    }
}

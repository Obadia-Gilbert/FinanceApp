using System.Collections;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FinanceApp.Infrastructure.Subscription;

/// <summary>
/// Validates Apple App Store JWS (StoreKit 2 transactions, ASN v2 signedPayload, nested signedTransactionInfo).
/// </summary>
internal static class AppleJwsVerifier
{
    public static bool TryReadVerifiedToken(
        string jws,
        IConfiguration config,
        ILogger logger,
        out JwtSecurityToken token)
    {
        token = null!;
        if (string.IsNullOrWhiteSpace(jws))
            return false;

        var allowUnsafe = config.GetValue("SubscriptionBilling:Apple:AllowUnsignedPayloadInDevelopment", false);

        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            if (allowUnsafe)
            {
                token = handler.ReadJwtToken(jws);
                logger.LogWarning("Apple JWS accepted without signature verification (AllowUnsignedPayloadInDevelopment).");
                return true;
            }

            var read = handler.ReadJwtToken(jws);
            if (!TryGetX5cLeaf(read, out var leafPem))
            {
                logger.LogWarning("Apple JWS missing x5c header.");
                return false;
            }

            var certBytes = Convert.FromBase64String(leafPem);
            using var leaf = X509CertificateLoader.LoadCertificate(certBytes);
            using var ecdsa = leaf.GetECDsaPublicKey();
            if (ecdsa == null)
            {
                logger.LogWarning("Apple leaf certificate has no ECDsa public key.");
                return false;
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

            handler.ValidateToken(jws, validation, out _);
            token = read;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to verify Apple JWS.");
            return false;
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

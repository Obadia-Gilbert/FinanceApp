using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinanceApp.Infrastructure.Subscription;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceApp.API.Tests.Unit;

public class AppleStoreTransactionVerifierTests
{
    [Fact]
    public async Task VerifySignedTransaction_ReturnsNull_WhenJwsEmpty()
    {
        var verifier = CreateVerifier(allowUnsafe: true);
        var result = await verifier.VerifySignedTransactionAsync("");
        Assert.Null(result);
    }

    [Fact]
    public async Task VerifySignedTransaction_ParsesPayload_WhenDevelopmentBypassEnabled()
    {
        var expiresMs = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeMilliseconds();
        var jws = BuildUnsignedJws(new Dictionary<string, object>
        {
            ["originalTransactionId"] = "orig-123",
            ["productId"] = "com.financeapp.mobile.pro.monthly",
            ["expiresDate"] = expiresMs,
            ["autoRenewStatus"] = 1
        });

        var verifier = CreateVerifier(allowUnsafe: true);
        var result = await verifier.VerifySignedTransactionAsync(jws);

        Assert.NotNull(result);
        Assert.Equal("orig-123", result!.OriginalTransactionId);
        Assert.Equal("com.financeapp.mobile.pro.monthly", result.ProductId);
        Assert.True(result.AutoRenewStatus);
    }

    private static AppleStoreTransactionVerifier CreateVerifier(bool allowUnsafe)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SubscriptionBilling:Apple:AllowUnsignedPayloadInDevelopment"] = allowUnsafe ? "true" : "false"
            })
            .Build();

        return new AppleStoreTransactionVerifier(config, NullLogger<AppleStoreTransactionVerifier>.Instance);
    }

    private static string BuildUnsignedJws(Dictionary<string, object> payloadClaims)
    {
        var claims = payloadClaims.Select(kv => new Claim(kv.Key, kv.Value.ToString()!)).ToList();
        var token = new JwtSecurityToken(claims: claims);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

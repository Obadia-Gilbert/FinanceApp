using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using Xunit;

namespace FinanceApp.API.Tests.Integration;

public class SubscriptionApiTests
{
    [Fact]
    public async Task VerifyApple_GrantsPro_WhenMockVerifierSucceeds()
    {
        var expires = DateTimeOffset.UtcNow.AddDays(30);
        var apple = new MockAppleVerifier(new AppleVerifiedTransaction(
            "orig-test-1",
            "com.financeapp.mobile.pro.monthly",
            expires,
            true));

        using var factory = new ApiWebApplicationFactory().WithSubscriptionVerifiers(apple: apple);
        var client = factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/subscription/verify/apple",
            new VerifyAppleSubscriptionRequest { SignedTransactionJws = "fake-jws" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sub = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.NotNull(sub);
        Assert.Equal("Pro", sub!.CurrentPlan);
        Assert.Equal("Apple", sub.BillingSource);
    }

    [Fact]
    public async Task VerifyGoogle_GrantsPro_WhenMockVerifierSucceeds()
    {
        var expires = DateTimeOffset.UtcNow.AddDays(30);
        var google = new MockGoogleVerifier(new GoogleVerifiedSubscription(
            "order-1",
            "pro_monthly",
            expires,
            true));

        using var factory = new ApiWebApplicationFactory().WithSubscriptionVerifiers(google: google);
        var client = factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/subscription/verify/google",
            new VerifyGoogleSubscriptionRequest
            {
                SubscriptionId = "pro_monthly",
                PurchaseToken = "purchase-token-abc"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sub = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.NotNull(sub);
        Assert.Equal("Pro", sub!.CurrentPlan);
        Assert.Equal("Google", sub.BillingSource);
    }

    [Fact]
    public async Task GetSubscription_Returns401_WhenNotAuthenticated()
    {
        using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/subscription");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<string> RegisterAndGetTokenAsync(HttpClient client)
    {
        var email = $"sub-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Sub", "User", email, "P@ssw0rd123!"));
        register.EnsureSuccessStatusCode();
        var login = await register.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.Token;
    }

    private sealed class MockAppleVerifier(AppleVerifiedTransaction result) : IAppleStoreTransactionVerifier
    {
        public Task<AppleVerifiedTransaction?> VerifySignedTransactionAsync(
            string signedTransactionJws,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AppleVerifiedTransaction?>(result);
    }

    private sealed class MockGoogleVerifier(GoogleVerifiedSubscription result) : IGooglePlaySubscriptionVerifier
    {
        public Task<GoogleVerifiedSubscription?> VerifySubscriptionAsync(
            string subscriptionId,
            string purchaseToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<GoogleVerifiedSubscription?>(result);
    }
}

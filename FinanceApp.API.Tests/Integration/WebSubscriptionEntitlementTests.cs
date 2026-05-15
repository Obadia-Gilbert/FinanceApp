using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceApp.API.Tests.Integration;

public class WebSubscriptionEntitlementTests
{
    [Fact]
    public async Task WebBilling_GrantsPro_AndMobileApiSeesSamePlan()
    {
        using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient();

        var email = $"web-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Web", "User", email, "P@ssw0rd123!"));
        register.EnsureSuccessStatusCode();
        var login = await register.Content.ReadFromJsonAsync<LoginResponse>();

        string userId;
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            userId = user!.Id;

            var entitlement = scope.ServiceProvider.GetRequiredService<ISubscriptionEntitlementService>();
            var result = await entitlement.ApplyVerifiedEntitlementAsync(
                userId,
                SubscriptionPlan.Pro,
                DateTimeOffset.UtcNow.AddDays(30),
                SubscriptionBillingSource.Web,
                externalTransactionId: "sub_web_test",
                productId: "price_pro_test",
                notes: "integration:web",
                googlePurchaseToken: "cus_web_test");

            Assert.True(result.Success);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        var sub = await client.GetFromJsonAsync<SubscriptionDto>("/api/subscription");
        Assert.NotNull(sub);
        Assert.Equal("Pro", sub!.CurrentPlan);
        Assert.Equal("Web", sub.BillingSource);
    }
}

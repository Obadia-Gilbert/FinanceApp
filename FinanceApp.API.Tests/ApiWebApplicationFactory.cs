using System.Collections.Generic;
using System.Linq;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceApp.API.Tests;

/// <summary>
/// WebApplicationFactory for the API project. Uses in-memory DB and test JWT config.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<FinanceApp.API.Program>
{
    private readonly string _sqlitePath = Path.Combine(Path.GetTempPath(), "FinanceAppTest_" + Guid.NewGuid().ToString("N") + ".db");
    private IEmailService? _emailServiceOverride;
    private IAppleStoreTransactionVerifier? _appleVerifierOverride;
    private IGooglePlaySubscriptionVerifier? _googleVerifierOverride;
    private IStripeBillingService? _stripeBillingOverride;

    /// <summary>
    /// Replace the registered <see cref="IEmailService"/> with a test double.
    /// Returns the same factory for fluent use:
    /// <c>new ApiWebApplicationFactory().WithEmailService(recorder)</c>.
    /// </summary>
    public ApiWebApplicationFactory WithEmailService(IEmailService emailService)
    {
        _emailServiceOverride = emailService;
        return this;
    }

    public ApiWebApplicationFactory WithSubscriptionVerifiers(
        IAppleStoreTransactionVerifier? apple = null,
        IGooglePlaySubscriptionVerifier? google = null)
    {
        _appleVerifierOverride = apple;
        _googleVerifierOverride = google;
        return this;
    }

    public ApiWebApplicationFactory WithStripeBilling(IStripeBillingService stripeBilling)
    {
        _stripeBillingOverride = stripeBilling;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-must-be-at-least-32-characters-long!",
                ["Jwt:Issuer"] = "FinanceApp.API.Tests",
                ["Jwt:Audience"] = "FinanceApp.Tests",
                ["Testing:SqlitePath"] = _sqlitePath,
                ["SubscriptionBilling:Apple:AllowUnsignedPayloadInDevelopment"] = "true",
                ["SubscriptionBilling:AppleProductIdToPlan:com.financeapp.mobile.pro.monthly"] = "Pro",
                ["SubscriptionBilling:GoogleProductIdToPlan:pro_monthly"] = "Pro",
                ["SubscriptionBilling:Stripe:PriceIdToPlan:price_pro_test"] = "Pro"
            });
        });

        builder.ConfigureServices(services =>
        {
            if (_emailServiceOverride is not null)
            {
                var existing = services.Where(d => d.ServiceType == typeof(IEmailService)).ToList();
                foreach (var d in existing) services.Remove(d);
                services.AddSingleton(_emailServiceOverride);
            }

            ReplaceScoped(services, _appleVerifierOverride);
            ReplaceScoped(services, _googleVerifierOverride);
            ReplaceScoped(services, _stripeBillingOverride);
        });
    }

    private static void ReplaceScoped<T>(IServiceCollection services, T? instance) where T : class
    {
        if (instance is null) return;
        var existing = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in existing) services.Remove(d);
        services.AddScoped(_ => instance);
    }
}

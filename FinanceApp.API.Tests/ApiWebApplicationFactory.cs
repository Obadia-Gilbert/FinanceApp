using System.Collections.Generic;
using System.Linq;
using FinanceApp.Application.Interfaces;
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
                ["Testing:SqlitePath"] = _sqlitePath
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
        });
    }
}

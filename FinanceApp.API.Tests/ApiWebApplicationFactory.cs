using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FinanceApp.API.Tests;

/// <summary>
/// WebApplicationFactory for the API project. Uses in-memory DB and test JWT config.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<FinanceApp.API.Program>
{
    private readonly string _sqlitePath = Path.Combine(Path.GetTempPath(), "FinanceAppTest_" + Guid.NewGuid().ToString("N") + ".db");

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
    }
}

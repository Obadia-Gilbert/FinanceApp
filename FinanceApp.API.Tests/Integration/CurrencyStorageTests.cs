using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceApp.API.Tests.Integration;

/// <summary>
/// Regression cover for the currency migration: the database column must hold the
/// ISO-4217 code itself ("TZS"), not the C# enum's ordinal (2) recast as text ("2") —
/// the exact corruption a naive int-&gt;string column type change would produce.
/// </summary>
public class CurrencyStorageTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public CurrencyStorageTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Budget_SavedWithTzs_IsStoredAndReadBackAsTzsCode()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var budget = new Budget($"user-{Guid.NewGuid():N}", 8, 2026, 100_000m, Currency.TZS);
        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        // Bypass EF's HasConversion<string>() entirely and read the raw stored value —
        // this is the check that would have caught storing "2" instead of "TZS".
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Currency FROM Budgets WHERE Id = @id";
        var idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = budget.Id;
        command.Parameters.Add(idParam);

        var rawValue = (string?)await command.ExecuteScalarAsync();

        Assert.Equal("TZS", rawValue);

        // And the round trip through EF resolves back to the correct enum member.
        db.ChangeTracker.Clear();
        var reloaded = await db.Budgets.FirstAsync(b => b.Id == budget.Id);
        Assert.Equal(Currency.TZS, reloaded.Currency);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceApp.API.DTOs;
using FinanceApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FinanceApp.API.Tests.Integration;

public class ExpensesApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    // The API now emits Currency (and other enums) as their string name, e.g. "TZS" —
    // default System.Text.Json options can't parse that back into an enum, so tests
    // that deserialize a DTO containing one need this converter, same as any real client.
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public ExpensesApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var email = $"exp-{Guid.NewGuid():N}@example.com";
        var password = "P@ssw0rd123!";
        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Exp", "User", email, password));
        registerRes.EnsureSuccessStatusCode();
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.Token;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task GetExpenses_Returns401_WithoutAuth()
    {
        var response = await _client.GetAsync("/api/expenses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExpenses_Returns200_WithAuth()
    {
        var token = await GetAuthTokenAsync();
        using var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync("/api/expenses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateExpense_Returns201_WithAuth()
    {
        var token = await GetAuthTokenAsync();
        using var authClient = CreateAuthenticatedClient(token);

        var categoriesResponse = await authClient.GetAsync("/api/categories");
        categoriesResponse.EnsureSuccessStatusCode();
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryDto>>();
        var categoryId = categories!.First().Id;

        var createRequest = new CreateExpenseRequest(99.99m, Currency.USD, DateTime.UtcNow.Date, categoryId, "Integration test expense");
        var response = await authClient.PostAsJsonAsync("/api/expenses", createRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var expense = await response.Content.ReadFromJsonAsync<ExpenseDto>(JsonOptions);
        Assert.NotNull(expense);
        Assert.Equal(99.99m, expense.Amount);
        Assert.Equal("Integration test expense", expense.Description);
    }

    /// <summary>
    /// Regression cover: Currency must serialize as its ISO-4217 code ("TZS"), not its
    /// enum ordinal (2) — a bare integer would silently break the moment a new currency
    /// is inserted anywhere but the very end of the enum.
    /// </summary>
    [Fact]
    public async Task CreateExpense_SerializesCurrencyAsIsoCode_NotOrdinal()
    {
        var token = await GetAuthTokenAsync();
        using var authClient = CreateAuthenticatedClient(token);

        var categoriesResponse = await authClient.GetAsync("/api/categories");
        categoriesResponse.EnsureSuccessStatusCode();
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryDto>>();
        var categoryId = categories!.First().Id;

        var createRequest = new CreateExpenseRequest(50m, Currency.TZS, DateTime.UtcNow.Date, categoryId, "Currency wire format test");
        var response = await authClient.PostAsJsonAsync("/api/expenses", createRequest);
        response.EnsureSuccessStatusCode();

        var rawJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"currency\":\"TZS\"", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"currency\":2", rawJson, StringComparison.OrdinalIgnoreCase);
    }
}

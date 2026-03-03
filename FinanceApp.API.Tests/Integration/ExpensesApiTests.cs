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
        var expense = await response.Content.ReadFromJsonAsync<ExpenseDto>();
        Assert.NotNull(expense);
        Assert.Equal(99.99m, expense.Amount);
        Assert.Equal("Integration test expense", expense.Description);
    }
}

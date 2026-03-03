using System.Net;
using System.Net.Http.Json;
using FinanceApp.API.DTOs;
using Xunit;

namespace FinanceApp.API.Tests.Integration;

public class AuthApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Returns201_WithToken()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest("Test", "User", email, "P@ssw0rd123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.False(string.IsNullOrEmpty(loginResponse.Token));
        Assert.Equal(email, loginResponse.Email);
    }

    [Fact]
    public async Task Register_Returns400_WhenEmailAlreadyExists()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest("Test", "User", email, "P@ssw0rd123!");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns200_WithToken_WhenValid()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        var password = "P@ssw0rd123!";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Login", "User", email, password));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.False(string.IsNullOrEmpty(loginResponse.Token));
    }

    [Fact]
    public async Task Login_Returns401_WhenInvalidPassword()
    {
        var email = $"badpwd-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("A", "B", email, "P@ssw0rd123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

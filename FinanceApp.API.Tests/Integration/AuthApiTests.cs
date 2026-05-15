using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces;
using FinanceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceApp.API.Tests.Integration;

public class AuthApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ApiWebApplicationFactory _factory;

    public AuthApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task ForgotPassword_Returns204_ForUnknownEmail()
    {
        // Should not reveal whether the address exists.
        var response = await _client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest($"unknown-{Guid.NewGuid():N}@example.com"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_Returns204_ForKnownEmail()
    {
        var email = $"forgot-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Forgot", "User", email, "P@ssw0rd123!"));

        var response = await _client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest(email));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_SendsEmail_ForKnownEmail()
    {
        // Spin up a dedicated factory that swaps IEmailService for a recorder
        // so we can assert the password-reset email is actually dispatched.
        var recorder = new RecordingEmailService();
        using var factory = new ApiWebApplicationFactory()
            .WithEmailService(recorder);
        var client = factory.CreateClient();

        var email = $"forgot-mock-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Forgot", "Mock", email, "P@ssw0rd123!"));

        // Registration also fires a branded welcome email; clear the recorder
        // so this assertion only sees the password-reset send we're testing.
        recorder.Sent.Clear();

        var response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest(email));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var sent = Assert.Single(recorder.Sent);
        Assert.Equal(email, sent.To);
        Assert.False(string.IsNullOrWhiteSpace(sent.Subject));
        Assert.False(string.IsNullOrWhiteSpace(sent.Body));
        // Body should include the reset URL with the email + base64url code.
        Assert.Contains("code=", sent.Body, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString(email), sent.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_SendsBrandedWelcomeEmail()
    {
        var recorder = new RecordingEmailService();
        using var factory = new ApiWebApplicationFactory()
            .WithEmailService(recorder);
        var client = factory.CreateClient();

        var email = $"welcome-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Welcome", "User", email, "P@ssw0rd123!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var welcome = Assert.Single(recorder.Sent);
        Assert.Equal(email, welcome.To);
        Assert.Contains("FinanceApp", welcome.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Welcome", welcome.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("branding/email-logo.png", welcome.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ForgotPassword_DoesNotSendEmail_ForUnknownEmail()
    {
        var recorder = new RecordingEmailService();
        using var factory = new ApiWebApplicationFactory()
            .WithEmailService(recorder);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest($"ghost-{Guid.NewGuid():N}@example.com"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(recorder.Sent);
    }

    [Fact]
    public async Task ForgotPassword_Returns400_ForInvalidEmail()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest("not-an-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_Returns204_AndAllowsLogin_WithValidToken()
    {
        var email = $"reset-{Guid.NewGuid():N}@example.com";
        var oldPassword = "Old@Password1!";
        var newPassword = "Brand@New2!";

        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Reset", "User", email, oldPassword));

        // Generate a real token via UserManager and base64url-encode it the same
        // way the controller does when emailing the link.
        string encodedToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            var rawToken = await userManager.GeneratePasswordResetTokenAsync(user!);
            encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        }

        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(email, encodedToken, newPassword));

        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var oldLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, oldPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, newPassword));
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_Returns400_WhenTokenInvalid()
    {
        var email = $"resetbad-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Reset", "User", email, "Old@Password1!"));

        var bogusToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("not-a-real-token"));

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(email, bogusToken, "Brand@New2!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_Returns400_ForUnknownEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest($"ghost-{Guid.NewGuid():N}@example.com",
                WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("token")),
                "Brand@New2!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------- Helpers --------

    /// <summary>Test double for <see cref="IEmailService"/> that records sends in memory.</summary>
    private sealed class RecordingEmailService : IEmailService
    {
        public ConcurrentBag<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendEmailAsync(string to, string subject, string body)
        {
            Sent.Add((to, subject, body));
            return Task.CompletedTask;
        }
    }
}

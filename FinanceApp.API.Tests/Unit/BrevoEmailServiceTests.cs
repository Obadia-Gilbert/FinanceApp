using System.Net;
using System.Text.Json;
using FinanceApp.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinanceApp.API.Tests.Unit;

public class BrevoEmailServiceTests
{
    [Fact]
    public async Task SendEmail_PostsExpectedPayloadToBrevoApi()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created, "{\"messageId\":\"<abc@brevo>\"}");
        var factory = new SingleClientFactory(new HttpClient(handler));

        var settings = Options.Create(new BrevoSettings
        {
            ApiKey = "xkeysib-test-key",
            SenderName = "FinanceApp",
            SenderEmail = "noreply@financeapp.test",
            ReplyToEmail = "support@financeapp.test",
            ReplyToName = "FinanceApp Support",
            TimeoutMs = 5000
        });

        var service = new BrevoEmailService(factory, settings);

        await service.SendEmailAsync("user@example.com", "Reset your password", "<p>Click here</p>");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(BrevoEmailService.SendEndpoint, handler.LastRequest.RequestUri!.ToString());

        Assert.True(handler.LastRequest.Headers.TryGetValues("api-key", out var keys));
        Assert.Equal("xkeysib-test-key", Assert.Single(keys));

        Assert.NotNull(handler.LastBody);
        using var doc = JsonDocument.Parse(handler.LastBody!);
        var root = doc.RootElement;

        Assert.Equal("FinanceApp", root.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("noreply@financeapp.test", root.GetProperty("sender").GetProperty("email").GetString());

        var to = root.GetProperty("to");
        Assert.Equal(1, to.GetArrayLength());
        Assert.Equal("user@example.com", to[0].GetProperty("email").GetString());

        Assert.Equal("Reset your password", root.GetProperty("subject").GetString());
        Assert.Equal("<p>Click here</p>", root.GetProperty("htmlContent").GetString());

        var replyTo = root.GetProperty("replyTo");
        Assert.Equal("support@financeapp.test", replyTo.GetProperty("email").GetString());
        Assert.Equal("FinanceApp Support", replyTo.GetProperty("name").GetString());
    }

    [Fact]
    public async Task SendEmail_OmitsReplyTo_WhenNotConfigured()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created, "{}");
        var factory = new SingleClientFactory(new HttpClient(handler));

        var settings = Options.Create(new BrevoSettings
        {
            ApiKey = "xkeysib-test-key",
            SenderEmail = "noreply@financeapp.test"
        });

        var service = new BrevoEmailService(factory, settings);

        await service.SendEmailAsync("user@example.com", "Hi", "<p>Hi</p>");

        using var doc = JsonDocument.Parse(handler.LastBody!);
        Assert.False(doc.RootElement.TryGetProperty("replyTo", out _));
    }

    [Fact]
    public async Task SendEmail_Throws_WhenApiKeyMissing()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
        var factory = new SingleClientFactory(new HttpClient(handler));
        var settings = Options.Create(new BrevoSettings { SenderEmail = "noreply@financeapp.test" });
        var service = new BrevoEmailService(factory, settings);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendEmailAsync("user@example.com", "s", "b"));
    }

    [Fact]
    public async Task SendEmail_ThrowsHttpException_OnNonSuccess()
    {
        var handler = new RecordingHandler(HttpStatusCode.Unauthorized, "{\"code\":\"unauthorized\"}");
        var factory = new SingleClientFactory(new HttpClient(handler));
        var settings = Options.Create(new BrevoSettings
        {
            ApiKey = "bad-key",
            SenderEmail = "noreply@financeapp.test"
        });
        var service = new BrevoEmailService(factory, settings);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SendEmailAsync("user@example.com", "s", "b"));
    }

    // -------- Helpers --------

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _responseBody;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        public RecordingHandler(HttpStatusCode status, string responseBody)
        {
            _status = status;
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public SingleClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }
}

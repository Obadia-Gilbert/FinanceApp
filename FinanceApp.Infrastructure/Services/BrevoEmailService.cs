using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinanceApp.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Sends transactional email via the Brevo (ex-Sendinblue) HTTP API.
/// Preferred over the SMTP <see cref="EmailService"/> in production because
/// it avoids SMTP handshakes, returns a messageId, and reports per-request
/// errors (e.g. unverified sender) directly in the HTTP response.
/// </summary>
public class BrevoEmailService : IEmailService
{
    public const string HttpClientName = "Brevo";
    public const string SendEndpoint = "https://api.brevo.com/v3/smtp/email";
    private const string ApiKeyHeader = "api-key";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BrevoSettings _settings;
    private readonly ILogger<BrevoEmailService>? _logger;

    public BrevoEmailService(
        IHttpClientFactory httpClientFactory,
        IOptions<BrevoSettings> options,
        ILogger<BrevoEmailService>? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Brevo:ApiKey is not configured.");
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
            throw new InvalidOperationException("Brevo:SenderEmail is not configured.");

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var timeoutMs = _settings.TimeoutMs > 0 ? _settings.TimeoutMs : 10000;
        client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

        var payload = new BrevoSendRequest
        {
            Sender = new BrevoAddress
            {
                Name = string.IsNullOrWhiteSpace(_settings.SenderName) ? null : _settings.SenderName,
                Email = _settings.SenderEmail
            },
            To = new[] { new BrevoAddress { Email = to } },
            Subject = subject,
            HtmlContent = body
        };

        if (!string.IsNullOrWhiteSpace(_settings.ReplyToEmail))
        {
            payload.ReplyTo = new BrevoAddress
            {
                Email = _settings.ReplyToEmail!,
                Name = string.IsNullOrWhiteSpace(_settings.ReplyToName) ? null : _settings.ReplyToName
            };
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        request.Headers.TryAddWithoutValidation(ApiKeyHeader, _settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await SafeReadAsync(response).ConfigureAwait(false);
            _logger?.LogError(
                "Brevo send failed: {Status} {Reason}. Body: {Body}",
                (int)response.StatusCode, response.ReasonPhrase, errorBody);
            throw new HttpRequestException(
                $"Brevo send failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage response)
    {
        try { return await response.Content.ReadAsStringAsync().ConfigureAwait(false); }
        catch { return string.Empty; }
    }

    // -------- DTOs (internal so tests can assert the wire shape) --------

    internal sealed class BrevoSendRequest
    {
        [JsonPropertyName("sender")] public BrevoAddress Sender { get; set; } = new();
        [JsonPropertyName("to")] public BrevoAddress[] To { get; set; } = Array.Empty<BrevoAddress>();
        [JsonPropertyName("subject")] public string Subject { get; set; } = string.Empty;
        [JsonPropertyName("htmlContent")] public string HtmlContent { get; set; } = string.Empty;
        [JsonPropertyName("replyTo")] public BrevoAddress? ReplyTo { get; set; }
    }

    internal sealed class BrevoAddress
    {
        [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}

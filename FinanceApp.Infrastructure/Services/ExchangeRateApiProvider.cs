using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Fetches live exchange rates from the free, no-key "Open Access" endpoint at
/// <see cref="ExchangeRateSettings.BaseUrl"/> (defaults to open.er-api.com, USD base).
/// </summary>
public class ExchangeRateApiProvider : IExchangeRateProvider
{
    public const string HttpClientName = "ExchangeRateApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExchangeRateSettings _settings;
    private readonly ILogger<ExchangeRateApiProvider> _logger;

    public ExchangeRateApiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExchangeRateSettings> options,
        ILogger<ExchangeRateApiProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<Currency, decimal>?> FetchRatesToUsdAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var timeoutMs = _settings.TimeoutMs > 0 ? _settings.TimeoutMs : 10000;
            client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

            using var response = await client.GetAsync(_settings.BaseUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Exchange rate fetch failed: {Status} {Reason}", (int)response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var payload = await response.Content
                .ReadFromJsonAsync<ExchangeRateApiResponse>(cancellationToken)
                .ConfigureAwait(false);

            if (payload is null || !string.Equals(payload.Result, "success", StringComparison.OrdinalIgnoreCase)
                || payload.Rates is null)
            {
                _logger.LogWarning("Exchange rate fetch returned an unexpected payload (result={Result})", payload?.Result);
                return null;
            }

            // The source reports "1 USD = X target"; the app's convention is
            // "1 unit of currency = X USD", so invert each rate.
            var ratesToUsd = new Dictionary<Currency, decimal>();
            foreach (var (code, unitsPerUsd) in payload.Rates)
            {
                if (unitsPerUsd <= 0) continue;
                if (!Enum.TryParse<Currency>(code, ignoreCase: true, out var currency)) continue;
                ratesToUsd[currency] = 1m / unitsPerUsd;
            }

            return ratesToUsd;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exchange rate fetch threw an exception");
            return null;
        }
    }

    private sealed class ExchangeRateApiResponse
    {
        [JsonPropertyName("result")] public string? Result { get; set; }
        [JsonPropertyName("rates")] public Dictionary<string, decimal>? Rates { get; set; }
    }
}

using FinanceApp.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Periodically fetches live exchange rates and pushes them into <see cref="IExchangeRateStore"/>.
/// The first fetch runs immediately at startup (before the first delay). A failed fetch is
/// logged and skipped — the store keeps serving its configured/default tier until the next
/// tick succeeds, so a network hiccup never blocks a conversion.
/// </summary>
public class ExchangeRateRefreshJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExchangeRateRefreshJob> _logger;
    private readonly TimeSpan _interval;

    public ExchangeRateRefreshJob(
        IServiceProvider services,
        IOptions<ExchangeRateSettings> options,
        ILogger<ExchangeRateRefreshJob> logger)
    {
        _services = services;
        _logger = logger;
        var hours = options.Value.RefreshIntervalHours > 0 ? options.Value.RefreshIntervalHours : 8;
        _interval = TimeSpan.FromHours(hours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Exchange rate refresh job started (interval: {Interval})", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<IExchangeRateProvider>();
                var store = scope.ServiceProvider.GetRequiredService<IExchangeRateStore>();

                var rates = await provider.FetchRatesToUsdAsync(stoppingToken);
                if (rates is { Count: > 0 })
                {
                    store.SetLiveRates(rates, DateTimeOffset.UtcNow);
                    _logger.LogInformation("Exchange rate refresh succeeded ({Count} currencies)", rates.Count);
                }
                else
                {
                    _logger.LogWarning("Exchange rate refresh returned no rates; keeping prior rates");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exchange rate refresh job error");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

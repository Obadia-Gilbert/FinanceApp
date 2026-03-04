using FinanceApp.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Runs periodically and creates transactions from due RecurringTemplates.
/// </summary>
public class RecurringTransactionJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RecurringTransactionJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public RecurringTransactionJob(IServiceProvider services, ILogger<RecurringTransactionJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recurring transaction job started (interval: {Interval})", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IRecurringTemplateService>();
                var count = await service.ProcessDueTemplatesAsync(DateTimeOffset.UtcNow);
                if (count > 0)
                    _logger.LogInformation("Recurring job created {Count} transaction(s)", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recurring transaction job error");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

using FinanceApp.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Periodically checks user daily activity and creates reminder notifications.
/// </summary>
public class DailyActivityReminderJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DailyActivityReminderJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public DailyActivityReminderJob(IServiceProvider services, ILogger<DailyActivityReminderJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily activity reminder job started (interval: {Interval})", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDailyActivityReminderService>();
                await service.EvaluateAndCreateDailyRemindersAsync(DateTimeOffset.UtcNow, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily activity reminder job error");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}


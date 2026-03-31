namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Evaluates users' daily activity and creates reminder notifications when needed.
/// </summary>
public interface IDailyActivityReminderService
{
    /// <summary>
    /// Creates at most one daily reminder notification per user/day when expense or income activity is missing.
    /// </summary>
    Task<int> EvaluateAndCreateDailyRemindersAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default);
}


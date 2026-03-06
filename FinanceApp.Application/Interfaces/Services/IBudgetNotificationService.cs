namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Evaluates budget and category budgets for a given month and creates in-app notifications
/// when limits are exceeded or warning threshold (80%) is reached. Call after expense create/update/delete
/// for real-time notifications, or when loading the dashboard.
/// </summary>
public interface IBudgetNotificationService
{
    /// <summary>
    /// For the given user and month/year, checks total and category budgets against current spend
    /// and creates notifications (at most one per scenario per month via topic keys).
    /// </summary>
    Task EvaluateAndCreateNotificationsAsync(string userId, int month, int year);
}

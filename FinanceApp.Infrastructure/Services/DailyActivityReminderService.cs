using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Creates daily reminders when users have not logged expense/income entries for the day.
/// </summary>
public class DailyActivityReminderService : IDailyActivityReminderService
{
    private readonly FinanceDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DailyActivityReminderService> _logger;

    public DailyActivityReminderService(
        FinanceDbContext db,
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService,
        ILogger<DailyActivityReminderService> logger)
    {
        _db = db;
        _userManager = userManager;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<int> EvaluateAndCreateDailyRemindersAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default)
    {
        var dayStartUtc = new DateTimeOffset(utcNow.UtcDateTime.Date, TimeSpan.Zero);
        var dayEndUtc = dayStartUtc.AddDays(1);
        var topicDate = dayStartUtc.ToString("yyyy-MM-dd");

        var candidateUserIds = await _userManager.Users
            .Where(u => u.DailyReminderEnabled)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var userId in candidateUserIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hasExpenseToday = await _db.Expenses
                .AnyAsync(e =>
                    !e.IsDeleted &&
                    e.UserId == userId &&
                    e.ExpenseDate >= dayStartUtc &&
                    e.ExpenseDate < dayEndUtc,
                    cancellationToken);

            var hasIncomeToday = await _db.Incomes
                .AnyAsync(i =>
                    !i.IsDeleted &&
                    i.UserId == userId &&
                    i.IncomeDate >= dayStartUtc &&
                    i.IncomeDate < dayEndUtc,
                    cancellationToken);

            if (hasExpenseToday && hasIncomeToday)
                continue;

            var (title, message, relatedLink, topicKey) = BuildReminder(hasExpenseToday, hasIncomeToday, topicDate);

            var createdNotification = await _notificationService.CreateIfNotExistsAsync(
                userId,
                title,
                message,
                NotificationType.Info,
                relatedLink,
                topicKey);

            if (createdNotification != null)
                created++;
        }

        if (created > 0)
            _logger.LogInformation("Daily activity reminder service created {Count} notifications for {Date}", created, topicDate);

        return created;
    }

    private static (string Title, string Message, string RelatedLink, string TopicKey) BuildReminder(bool hasExpense, bool hasIncome, string dateKey)
    {
        if (!hasExpense && !hasIncome)
        {
            return (
                "Daily reminder",
                "You have not logged any expense or income today. Add at least one entry to keep your records up to date.",
                "/Home/Index",
                $"daily-log-reminder-both-{dateKey}");
        }

        if (!hasExpense)
        {
            return (
                "Daily expense reminder",
                "You have not logged an expense today. Add one expense entry to keep your daily tracking accurate.",
                "/Expense/Index",
                $"daily-log-reminder-expense-{dateKey}");
        }

        return (
            "Daily income reminder",
            "You have not logged income today. Add one income entry if you received any funds.",
            "/Income/Index",
            $"daily-log-reminder-income-{dateKey}");
    }
}


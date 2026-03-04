using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Template for recurring income or expense. A background job creates Transaction (and optionally Income)
/// instances on schedule; NextRunDate is advanced after each run.
/// </summary>
public class RecurringTemplate : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public string? Note { get; private set; }
    public RecurrenceFrequency Frequency { get; private set; }
    /// <summary>Interval (e.g. every 2 weeks = 2 when Frequency is Weekly).</summary>
    public int Interval { get; private set; } = 1;
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    /// <summary>Next date to generate a transaction.</summary>
    public DateTimeOffset NextRunDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Account Account { get; private set; } = null!;
    public Category? Category { get; private set; }

    protected RecurringTemplate() { }

    public RecurringTemplate(
        string userId,
        Guid accountId,
        TransactionType type,
        decimal amount,
        Currency currency,
        RecurrenceFrequency frequency,
        DateTimeOffset startDate,
        Guid? categoryId = null,
        string? note = null,
        int interval = 1,
        DateTimeOffset? endDate = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        if (type == TransactionType.Transfer) throw new ArgumentException("Recurring templates support Income or Expense only.", nameof(type));
        if (interval < 1) throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be at least 1.");

        UserId = userId;
        AccountId = accountId;
        CategoryId = categoryId;
        Type = type;
        Amount = amount;
        Currency = currency;
        Note = note;
        Frequency = frequency;
        Interval = interval;
        StartDate = startDate;
        EndDate = endDate;
        NextRunDate = startDate;
    }

    public void AdvanceNextRunDate()
    {
        NextRunDate = Frequency switch
        {
            RecurrenceFrequency.Weekly => NextRunDate.AddDays(7 * Interval),
            RecurrenceFrequency.Monthly => NextRunDate.AddMonths(Interval),
            RecurrenceFrequency.Yearly => NextRunDate.AddYears(Interval),
            _ => NextRunDate.AddMonths(Interval)
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void UpdateAmount(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        Amount = amount;
    }
    public void UpdateNote(string? note) => Note = note;
}

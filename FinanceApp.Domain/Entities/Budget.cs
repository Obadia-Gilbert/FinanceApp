using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// User's monthly budget limit. When expenses in the same month/currency reach this amount, the user should be alerted.
/// </summary>
public class Budget : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public int Month { get; private set; }
    public int Year { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }

    protected Budget() { }

    public Budget(string userId, int month, int year, decimal amount, Currency currency)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be 1–12.");
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be 2000–2100.");
        if (amount < 0)
            throw new ArgumentException("Budget amount cannot be negative.", nameof(amount));

        UserId = userId;
        Month = month;
        Year = year;
        Amount = amount;
        Currency = currency;
    }

    public void UpdateAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Budget amount cannot be negative.", nameof(amount));
        Amount = amount;
    }
}

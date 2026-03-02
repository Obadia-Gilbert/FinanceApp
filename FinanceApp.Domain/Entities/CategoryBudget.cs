using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// User's optional budget limit for a specific category in a given month.
/// When expenses in that category (same month/year/currency) reach this amount, the user is alerted.
/// </summary>
public class CategoryBudget : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public int Month { get; private set; }
    public int Year { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }

    // Navigation property
    public Category Category { get; private set; } = null!;

    protected CategoryBudget() { }

    public CategoryBudget(string userId, Guid categoryId, int month, int year, decimal amount, Currency currency)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be 1–12.");
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be 2000–2100.");
        if (amount < 0)
            throw new ArgumentException("Budget amount cannot be negative.", nameof(amount));

        UserId = userId;
        CategoryId = categoryId;
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

    public void UpdateAmountAndCurrency(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Budget amount cannot be negative.", nameof(amount));
        Amount = amount;
        Currency = currency;
    }
}

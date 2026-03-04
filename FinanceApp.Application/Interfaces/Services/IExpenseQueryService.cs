using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

/// <summary>Efficient aggregated and bounded expense queries for dashboard and reports.</summary>
public interface IExpenseQueryService
{
    /// <summary>Spend per (CategoryId, Currency) for the given month (single grouped query).</summary>
    Task<Dictionary<(Guid CategoryId, Currency Currency), decimal>> GetCategorySpendForMonthAsync(string userId, int month, int year);

    /// <summary>Total amount per currency, optionally filtered by date range.</summary>
    Task<Dictionary<Currency, decimal>> GetTotalsByCurrencyAsync(string userId, DateTimeOffset? from = null, DateTimeOffset? to = null);

    /// <summary>Total per currency for a single month.</summary>
    Task<Dictionary<Currency, decimal>> GetMonthTotalsByCurrencyAsync(string userId, int month, int year);

    /// <summary>Sum by day in range (for trend chart). Optional currency filter.</summary>
    Task<IReadOnlyList<ExpenseTotalByDayDto>> GetSumsByDayAsync(string userId, DateTimeOffset from, DateTimeOffset to, Currency? currency = null);

    /// <summary>Category totals for a month (for top-N breakdown). Optional currency filter.</summary>
    Task<IReadOnlyList<CategoryTotalDto>> GetCategoryTotalsForMonthAsync(string userId, int month, int year, Currency? currency = null);

    /// <summary>Most recent N expenses (bounded query) with Category included.</summary>
    Task<IReadOnlyList<Expense>> GetRecentExpensesAsync(string userId, int count);

    /// <summary>Total expense count for the user (for dashboard).</summary>
    Task<int> GetTotalCountAsync(string userId);

    /// <summary>Expense count for a given month (for MoM count change).</summary>
    Task<int> GetMonthExpenseCountAsync(string userId, int month, int year);

    /// <summary>Top N expenses for a month by amount (for report).</summary>
    Task<IReadOnlyList<Expense>> GetTopExpensesForMonthAsync(string userId, int month, int year, int count);
}

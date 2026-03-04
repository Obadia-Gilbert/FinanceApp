using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

public interface ICategoryBudgetService
{
    /// <summary>Gets all category budgets for a user for the given month/year.</summary>
    Task<IEnumerable<CategoryBudget>> GetForMonthAsync(string userId, int month, int year);

    /// <summary>Gets the category budget for a specific category in a month, or null if not set.</summary>
    Task<CategoryBudget?> GetForCategoryAsync(string userId, Guid categoryId, int month, int year);

    /// <summary>Sets or updates a category budget. One budget per user per category per month (upsert).</summary>
    Task<CategoryBudget> SetAsync(string userId, Guid categoryId, int month, int year, decimal amount, Currency currency);

    /// <summary>Deletes a category budget by id if it belongs to the user.</summary>
    Task<bool> DeleteAsync(Guid id, string userId);

    /// <summary>Gets the total spent in a category for the given month/year in the specified currency.</summary>
    Task<decimal> GetCategorySpendAsync(string userId, Guid categoryId, int month, int year, Currency currency);

    /// <summary>Gets spend per (CategoryId, Currency) for the month in one query. Use for dashboard/report to avoid N+1.</summary>
    Task<Dictionary<(Guid CategoryId, Currency Currency), decimal>> GetCategorySpendForMonthAsync(string userId, int month, int year);
}

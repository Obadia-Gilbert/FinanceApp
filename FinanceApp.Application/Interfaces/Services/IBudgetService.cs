using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

public interface IBudgetService
{
    /// <summary>Gets the budget for a user for the given month/year, or null if not set.</summary>
    Task<Budget?> GetBudgetForMonthAsync(string userId, int month, int year);

    /// <summary>Sets or updates the monthly budget. One budget per user per month (upsert).</summary>
    Task<Budget> SetBudgetAsync(string userId, int month, int year, decimal amount, Currency currency);
}

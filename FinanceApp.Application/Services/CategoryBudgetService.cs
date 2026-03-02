using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Services;

public class CategoryBudgetService : ICategoryBudgetService
{
    private readonly IRepository<CategoryBudget> _categoryBudgetRepository;
    private readonly IRepository<Expense> _expenseRepository;

    public CategoryBudgetService(
        IRepository<CategoryBudget> categoryBudgetRepository,
        IRepository<Expense> expenseRepository)
    {
        _categoryBudgetRepository = categoryBudgetRepository ?? throw new ArgumentNullException(nameof(categoryBudgetRepository));
        _expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
    }

    public async Task<IEnumerable<CategoryBudget>> GetForMonthAsync(string userId, int month, int year)
    {
        if (string.IsNullOrWhiteSpace(userId)) return [];
        var paged = await _categoryBudgetRepository.GetPagedAsync(
            pageNumber: 1,
            pageSize: 500,
            filter: cb => cb.UserId == userId && cb.Month == month && cb.Year == year,
            orderBy: q => q.OrderBy(cb => cb.CategoryId),
            cb => cb.Category!);
        return paged.Items;
    }

    public async Task<CategoryBudget?> GetForCategoryAsync(string userId, Guid categoryId, int month, int year)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var list = await _categoryBudgetRepository.FindAsync(cb =>
            cb.UserId == userId && cb.CategoryId == categoryId && cb.Month == month && cb.Year == year);
        return list.FirstOrDefault();
    }

    public async Task<CategoryBudget> SetAsync(string userId, Guid categoryId, int month, int year, decimal amount, Currency currency)
    {
        var existing = await GetForCategoryAsync(userId, categoryId, month, year);
        if (existing != null)
        {
            existing.UpdateAmountAndCurrency(amount, currency);
            _categoryBudgetRepository.Update(existing);
            await _categoryBudgetRepository.SaveChangesAsync();
            return existing;
        }
        var budget = new CategoryBudget(userId, categoryId, month, year, amount, currency);
        await _categoryBudgetRepository.AddAsync(budget);
        await _categoryBudgetRepository.SaveChangesAsync();
        return budget;
    }

    public async Task<bool> DeleteAsync(Guid id, string userId)
    {
        var budget = await _categoryBudgetRepository.GetByIdAsync(id);
        if (budget == null || budget.UserId != userId) return false;
        _categoryBudgetRepository.Remove(budget);
        await _categoryBudgetRepository.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetCategorySpendAsync(string userId, Guid categoryId, int month, int year, Currency currency)
    {
        if (string.IsNullOrWhiteSpace(userId)) return 0;
        var expenses = await _expenseRepository.FindAsync(e =>
            e.UserId == userId &&
            e.CategoryId == categoryId &&
            e.Currency == currency &&
            e.ExpenseDate.Month == month &&
            e.ExpenseDate.Year == year);
        return expenses.Sum(e => e.Amount);
    }
}

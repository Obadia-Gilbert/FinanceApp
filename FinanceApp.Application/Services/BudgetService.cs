using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Services;

public class BudgetService : IBudgetService
{
    private readonly IRepository<Budget> _budgetRepository;

    public BudgetService(IRepository<Budget> budgetRepository)
    {
        _budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
    }

    public async Task<Budget?> GetBudgetForMonthAsync(string userId, int month, int year)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var list = await _budgetRepository.FindAsync(b => b.UserId == userId && b.Month == month && b.Year == year);
        return list.FirstOrDefault();
    }

    public async Task<Budget> SetBudgetAsync(string userId, int month, int year, decimal amount, Currency currency)
    {
        var existing = await GetBudgetForMonthAsync(userId, month, year);
        if (existing != null)
        {
            existing.UpdateAmount(amount);
            _budgetRepository.Update(existing);
            await _budgetRepository.SaveChangesAsync();
            return existing;
        }
        var budget = new Budget(userId, month, year, amount, currency);
        await _budgetRepository.AddAsync(budget);
        await _budgetRepository.SaveChangesAsync();
        return budget;
    }
}

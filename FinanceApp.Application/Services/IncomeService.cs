using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Application.Extensions;
using System.Linq.Expressions;

namespace FinanceApp.Application.Services;

public class IncomeService : IIncomeService
{
    private readonly IRepository<Income> _repository;
    private readonly ITransactionService _transactionService;

    public IncomeService(IRepository<Income> repository, ITransactionService transactionService)
    {
        _repository = repository;
        _transactionService = transactionService;
    }

    public async Task<Income?> GetByIdAsync(Guid id, string userId)
    {
        var income = await _repository.GetByIdAsync(id);
        return income?.UserId == userId ? income : null;
    }

    public async Task<PagedResult<Income>> GetPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        Expression<Func<Income, bool>>? filter = null)
    {
        Expression<Func<Income, bool>> userFilter = i => i.UserId == userId;
        if (filter != null)
            userFilter = userFilter.AndAlso(filter);

        return await _repository.GetPagedAsync(
            pageNumber,
            pageSize,
            userFilter,
            q => q.OrderByDescending(i => i.IncomeDate),
            i => i.Account,
            i => i.Category);
    }

    public async Task<Income> CreateAsync(
        string userId,
        Guid? accountId,
        Guid categoryId,
        decimal amount,
        Currency currency,
        DateTimeOffset incomeDate,
        string? description = null,
        string? source = null)
    {
        Guid? transactionId = null;
        if (accountId.HasValue)
        {
            var transaction = await _transactionService.CreateAsync(
                userId, accountId.Value, TransactionType.Income, amount, currency, incomeDate,
                categoryId, description ?? source, isRecurring: false);
            transactionId = transaction.Id;
        }

        var income = new Income(userId, accountId, categoryId, amount, currency, incomeDate, description, source, transactionId);
        await _repository.AddAsync(income);
        await _repository.SaveChangesAsync();
        return income;
    }

    public async Task UpdateAsync(
        Guid id,
        string userId,
        decimal amount,
        DateTimeOffset incomeDate,
        Guid? accountId,
        Guid categoryId,
        string? description = null,
        string? source = null)
    {
        var income = await GetByIdAsync(id, userId)
            ?? throw new InvalidOperationException("Income not found.");

        income.UpdateAmount(amount);
        income.UpdateIncomeDate(incomeDate);
        income.UpdateAccount(accountId);
        income.UpdateCategory(categoryId);
        income.UpdateDescription(description);
        income.UpdateSource(source);
        _repository.Update(income);

        if (income.TransactionId.HasValue)
        {
            await _transactionService.UpdateAsync(
                income.TransactionId.Value,
                userId,
                amount,
                incomeDate,
                categoryId,
                description ?? source);
        }

        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var income = await GetByIdAsync(id, userId)
            ?? throw new InvalidOperationException("Income not found.");

        if (income.TransactionId.HasValue)
            await _transactionService.DeleteAsync(income.TransactionId.Value, userId);

        _repository.SoftDelete(income);
        await _repository.SaveChangesAsync();
    }
}

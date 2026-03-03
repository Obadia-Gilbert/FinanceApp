using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Extensions;
using System.Linq.Expressions;

namespace FinanceApp.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly IRepository<Transaction> _repository;

    public TransactionService(IRepository<Transaction> repository)
    {
        _repository = repository;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, string userId)
    {
        var t = await _repository.GetByIdAsync(id);
        return t?.UserId == userId ? t : null;
    }

    public async Task<PagedResult<Transaction>> GetPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        Expression<Func<Transaction, bool>>? filter = null)
    {
        Expression<Func<Transaction, bool>> userFilter = t => t.UserId == userId;

        if (filter != null)
            userFilter = userFilter.AndAlso(filter);

        return await _repository.GetPagedAsync(
            pageNumber,
            pageSize,
            userFilter,
            q => q.OrderByDescending(t => t.Date),
            t => t.Account,
            t => t.Category!);
    }

    public async Task<Transaction> CreateAsync(
        string userId, Guid accountId, TransactionType type, decimal amount,
        Currency currency, DateTimeOffset date, Guid? categoryId = null,
        string? note = null, bool isRecurring = false)
    {
        var transaction = new Transaction(userId, accountId, type, amount, currency, date, categoryId, note, null, isRecurring);
        await _repository.AddAsync(transaction);
        await _repository.SaveChangesAsync();
        return transaction;
    }

    public async Task<(Transaction From, Transaction To)> CreateTransferAsync(
        string userId, Guid fromAccountId, Guid toAccountId,
        decimal amount, Currency currency, DateTimeOffset date, string? note = null)
    {
        var groupId = Guid.NewGuid();

        var from = new Transaction(userId, fromAccountId, TransactionType.Transfer, amount, currency, date, null, note, groupId);
        var to = new Transaction(userId, toAccountId, TransactionType.Transfer, amount, currency, date, null, note, groupId);

        await _repository.AddAsync(from);
        await _repository.AddAsync(to);
        await _repository.SaveChangesAsync();

        return (from, to);
    }

    public async Task UpdateAsync(Guid id, string userId, decimal amount, DateTimeOffset date, Guid? categoryId, string? note)
    {
        var transaction = await GetByIdAsync(id, userId)
            ?? throw new InvalidOperationException("Transaction not found.");

        transaction.UpdateAmount(amount);
        transaction.UpdateDate(date);
        transaction.UpdateCategory(categoryId);
        transaction.UpdateNote(note);

        _repository.Update(transaction);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var transaction = await GetByIdAsync(id, userId)
            ?? throw new InvalidOperationException("Transaction not found.");
        _repository.SoftDelete(transaction);
        await _repository.SaveChangesAsync();
    }
}

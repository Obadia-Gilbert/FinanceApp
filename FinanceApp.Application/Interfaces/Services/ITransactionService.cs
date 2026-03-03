using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using System.Linq.Expressions;

namespace FinanceApp.Application.Interfaces.Services;

public interface ITransactionService
{
    Task<Transaction?> GetByIdAsync(Guid id, string userId);

    Task<PagedResult<Transaction>> GetPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        Expression<Func<Transaction, bool>>? filter = null);

    /// <summary>Creates a single Income or Expense transaction.</summary>
    Task<Transaction> CreateAsync(
        string userId,
        Guid accountId,
        TransactionType type,
        decimal amount,
        Currency currency,
        DateTimeOffset date,
        Guid? categoryId = null,
        string? note = null,
        bool isRecurring = false);

    /// <summary>
    /// Creates a Transfer: two linked transactions (debit from source, credit to destination)
    /// sharing the same TransferGroupId. Atomic — both records succeed or neither does.
    /// </summary>
    Task<(Transaction From, Transaction To)> CreateTransferAsync(
        string userId,
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        Currency currency,
        DateTimeOffset date,
        string? note = null);

    Task UpdateAsync(Guid id, string userId, decimal amount, DateTimeOffset date, Guid? categoryId, string? note);
    Task DeleteAsync(Guid id, string userId);
}

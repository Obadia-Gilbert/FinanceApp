using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using System.Linq.Expressions;

namespace FinanceApp.Application.Interfaces.Services;

public interface IIncomeService
{
    Task<Income?> GetByIdAsync(Guid id, string userId);
    Task<PagedResult<Income>> GetPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        Expression<Func<Income, bool>>? filter = null);

    /// <summary>Creates an Income. If accountId is set, also creates a Transaction so account balance updates.</summary>
    Task<Income> CreateAsync(
        string userId,
        Guid? accountId,
        Guid categoryId,
        decimal amount,
        Currency currency,
        DateTimeOffset incomeDate,
        string? description = null,
        string? source = null);

    /// <summary>Updates the Income and the linked Transaction if present.</summary>
    Task UpdateAsync(
        Guid id,
        string userId,
        decimal amount,
        DateTimeOffset incomeDate,
        Guid? accountId,
        Guid categoryId,
        string? description = null,
        string? source = null);

    /// <summary>Soft-deletes the Income and the linked Transaction if present.</summary>
    Task DeleteAsync(Guid id, string userId);
}

using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using System.Linq.Expressions;

namespace FinanceApp.Application.Interfaces.Services;

public interface IRecurringTemplateService
{
    Task<RecurringTemplate?> GetByIdAsync(Guid id, string userId);
    Task<PagedResult<RecurringTemplate>> GetPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        Expression<Func<RecurringTemplate, bool>>? filter = null);

    Task<RecurringTemplate> CreateAsync(
        string userId,
        Guid accountId,
        TransactionType type,
        decimal amount,
        Currency currency,
        RecurrenceFrequency frequency,
        DateTimeOffset startDate,
        Guid? categoryId = null,
        string? note = null,
        int interval = 1,
        DateTimeOffset? endDate = null);

    Task UpdateAsync(Guid id, string userId, decimal amount, string? note = null);
    Task DeactivateAsync(Guid id, string userId);
    Task DeleteAsync(Guid id, string userId);

    /// <summary>Processes all due templates (NextRunDate &lt;= today): creates Transaction and advances NextRunDate.</summary>
    Task<int> ProcessDueTemplatesAsync(DateTimeOffset upToDate);
}

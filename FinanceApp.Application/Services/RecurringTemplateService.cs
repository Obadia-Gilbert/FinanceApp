using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Application.Extensions;
using System.Linq.Expressions;

namespace FinanceApp.Application.Services;

public class RecurringTemplateService : IRecurringTemplateService
{
    private readonly IRepository<RecurringTemplate> _repository;
    private readonly ITransactionService _transactionService;

    public RecurringTemplateService(
        IRepository<RecurringTemplate> repository,
        ITransactionService transactionService)
    {
        _repository = repository;
        _transactionService = transactionService;
    }

    public async Task<RecurringTemplate?> GetByIdAsync(Guid id, string userId)
    {
        var t = await _repository.GetByIdAsync(id);
        return t?.UserId == userId ? t : null;
    }

    public async Task<PagedResult<RecurringTemplate>> GetPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        Expression<Func<RecurringTemplate, bool>>? filter = null)
    {
        Expression<Func<RecurringTemplate, bool>> userFilter = r => r.UserId == userId && r.IsActive;
        if (filter != null)
            userFilter = userFilter.AndAlso(filter);

        return await _repository.GetPagedAsync(
            pageNumber,
            pageSize,
            userFilter,
            q => q.OrderBy(r => r.NextRunDate),
            r => r.Account,
            r => r.Category!);
    }

    public async Task<RecurringTemplate> CreateAsync(
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
        DateTimeOffset? endDate = null)
    {
        var template = new RecurringTemplate(userId, accountId, type, amount, currency, frequency, startDate, categoryId, note, interval, endDate);
        await _repository.AddAsync(template);
        await _repository.SaveChangesAsync();
        return template;
    }

    public async Task UpdateAsync(Guid id, string userId, decimal amount, string? note = null)
    {
        var template = await GetByIdAsync(id, userId)
            ?? throw new InvalidOperationException("Recurring template not found.");
        template.UpdateAmount(amount);
        template.UpdateNote(note);
        _repository.Update(template);
        await _repository.SaveChangesAsync();
    }

    public async Task DeactivateAsync(Guid id, string userId)
    {
        var template = await GetByIdAsync(id, userId)
            ?? throw new InvalidOperationException("Recurring template not found.");
        template.Deactivate();
        _repository.Update(template);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var template = await GetByIdAsync(id, userId)
            ?? throw new InvalidOperationException("Recurring template not found.");
        _repository.SoftDelete(template);
        await _repository.SaveChangesAsync();
    }

    public async Task<int> ProcessDueTemplatesAsync(DateTimeOffset upToDate)
    {
        var due = await _repository.FindAsync(r =>
            r.IsActive &&
            r.NextRunDate <= upToDate &&
            (r.EndDate == null || r.NextRunDate <= r.EndDate) &&
            r.Type != TransactionType.Transfer);

        var count = 0;
        foreach (var template in due)
        {
            try
            {
                await _transactionService.CreateAsync(
                    template.UserId,
                    template.AccountId,
                    template.Type,
                    template.Amount,
                    template.Currency,
                    template.NextRunDate,
                    template.CategoryId,
                    template.Note,
                    isRecurring: true);

                template.AdvanceNextRunDate();
                if (template.EndDate.HasValue && template.NextRunDate > template.EndDate.Value)
                    template.Deactivate();
                _repository.Update(template);
                count++;
            }
            catch
            {
                // Skip this template on error; will retry next run
            }
        }

        if (count > 0)
            await _repository.SaveChangesAsync();

        return count;
    }
}

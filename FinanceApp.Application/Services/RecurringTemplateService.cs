using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Application.Extensions;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Services;

public class RecurringTemplateService : IRecurringTemplateService
{
    private readonly IRepository<RecurringTemplate> _repository;
    private readonly ITransactionService _transactionService;
    private readonly IExpenseService _expenseService;
    private readonly IIncomeService _incomeService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<RecurringTemplateService> _logger;

    public RecurringTemplateService(
        IRepository<RecurringTemplate> repository,
        ITransactionService transactionService,
        IExpenseService expenseService,
        IIncomeService incomeService,
        ICategoryService categoryService,
        ILogger<RecurringTemplateService> logger)
    {
        _repository = repository;
        _transactionService = transactionService;
        _expenseService = expenseService;
        _incomeService = incomeService;
        _categoryService = categoryService;
        _logger = logger;
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

        // Generate the first occurrence immediately if StartDate is today or in the past
        if (template.NextRunDate <= DateTimeOffset.UtcNow)
        {
            var created = await GenerateOccurrenceAsync(template);
            if (created)
            {
                template.AdvanceNextRunDate();
                if (template.EndDate.HasValue && template.NextRunDate > template.EndDate.Value)
                    template.Deactivate();
            }
            _repository.Update(template);
            await _repository.SaveChangesAsync();
        }

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
                var created = await GenerateOccurrenceAsync(template);
                if (!created)
                {
                    _repository.Update(template);
                    count++;
                    continue;
                }

                template.AdvanceNextRunDate();
                if (template.EndDate.HasValue && template.NextRunDate > template.EndDate.Value)
                    template.Deactivate();
                _repository.Update(template);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Recurring template {TemplateId} skipped; will retry on next run", template.Id);
            }
        }

        if (count > 0)
            await _repository.SaveChangesAsync();

        return count;
    }

    /// <summary>
    /// Creates the proper Income or Expense record for a recurring template occurrence.
    /// Returns false if the template was deactivated (missing category) and no row was created.
    /// </summary>
    private async Task<bool> GenerateOccurrenceAsync(RecurringTemplate template)
    {
        if (!await EnsureCategoryValidForOccurrenceAsync(template))
            return false;

        var categoryId = template.CategoryId!.Value;

        if (template.Type == TransactionType.Expense)
        {
            await _expenseService.CreateExpenseAsync(
                amount: template.Amount,
                currency: template.Currency,
                expenseDate: template.NextRunDate.DateTime,
                categoryId: categoryId,
                userId: template.UserId,
                description: template.Note ?? "Recurring expense",
                receiptPath: null,
                accountId: template.AccountId);
        }
        else if (template.Type == TransactionType.Income)
        {
            await _incomeService.CreateAsync(
                userId: template.UserId,
                accountId: template.AccountId,
                categoryId: categoryId,
                amount: template.Amount,
                currency: template.Currency,
                incomeDate: template.NextRunDate,
                description: template.Note,
                source: "Recurring");
        }

        return true;
    }

    /// <summary>Expense/Income recurring rows require a category that still exists for the user.</summary>
    private async Task<bool> EnsureCategoryValidForOccurrenceAsync(RecurringTemplate template)
    {
        if (template.Type != TransactionType.Expense && template.Type != TransactionType.Income)
            return true;

        if (!template.CategoryId.HasValue || template.CategoryId.Value == Guid.Empty)
        {
            _logger.LogWarning(
                "Recurring template {TemplateId} deactivated: category is required for recurring {Type}",
                template.Id, template.Type);
            template.Deactivate();
            return false;
        }

        var category = await _categoryService.GetByIdAsync(template.CategoryId.Value, template.UserId);
        if (category == null)
        {
            _logger.LogWarning(
                "Recurring template {TemplateId} deactivated: category {CategoryId} not found (deleted or wrong user)",
                template.Id, template.CategoryId);
            template.Deactivate();
            return false;
        }

        return true;
    }
}

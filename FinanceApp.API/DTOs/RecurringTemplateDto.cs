using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record RecurringTemplateDto(
    Guid Id,
    Guid AccountId,
    string? AccountName,
    Guid? CategoryId,
    string? CategoryName,
    int Type,           // TransactionType enum
    decimal Amount,
    int Currency,       // Currency enum
    int Frequency,      // RecurrenceFrequency enum
    int Interval,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset NextRunDate,
    string? Note,
    bool IsActive);

public record CreateRecurringTemplateRequest(
    Guid AccountId,
    Guid? CategoryId,
    int Type,           // TransactionType: 0=Income, 1=Expense
    decimal Amount,
    int Currency,
    int Frequency,      // RecurrenceFrequency: 0=Weekly, 1=Monthly, 2=Yearly
    string StartDate,   // ISO date
    string? EndDate,
    int Interval = 1,
    string? Note = null);

public record UpdateRecurringTemplateRequest(decimal Amount, string? Note);

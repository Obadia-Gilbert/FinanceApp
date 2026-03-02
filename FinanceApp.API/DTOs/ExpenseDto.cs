using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record ExpenseDto(
    Guid Id,
    decimal Amount,
    Currency Currency,
    DateTimeOffset ExpenseDate,
    string? Description,
    Guid CategoryId,
    string? CategoryName,
    string? ReceiptPath,
    DateTimeOffset CreatedAt);

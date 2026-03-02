using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record CategoryBudgetDto(
    Guid Id,
    Guid CategoryId,
    string? CategoryName,
    int Month,
    int Year,
    decimal Amount,
    Currency Currency,
    decimal Spent);

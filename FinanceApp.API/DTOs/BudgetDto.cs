using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record BudgetDto(Guid Id, int Month, int Year, decimal Amount, Currency Currency);

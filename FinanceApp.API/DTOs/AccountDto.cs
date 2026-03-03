using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record AccountDto(Guid Id, string Name, AccountType Type, Currency Currency, decimal InitialBalance, decimal CurrentBalance, string? Description, bool IsActive);

public record CreateAccountRequest(string Name, AccountType Type, Currency Currency, decimal InitialBalance, string? Description = null);

public record UpdateAccountRequest(string Name, string? Description = null);

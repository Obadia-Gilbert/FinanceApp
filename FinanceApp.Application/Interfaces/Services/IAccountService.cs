using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

public interface IAccountService
{
    Task<Account?> GetByIdAsync(Guid id, string userId);
    Task<IEnumerable<Account>> GetAllAsync(string userId);
    Task<Account> CreateAsync(string userId, string name, AccountType type, Currency currency, decimal initialBalance, string? description = null);
    Task UpdateAsync(Guid id, string userId, string name, string? description = null);
    Task DeactivateAsync(Guid id, string userId);

    /// <summary>Computes current balance: InitialBalance + SUM of transactions.</summary>
    Task<decimal> GetBalanceAsync(Guid accountId, string userId);
}

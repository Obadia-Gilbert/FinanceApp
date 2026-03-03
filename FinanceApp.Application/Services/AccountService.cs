using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Services;

public class AccountService : IAccountService
{
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Transaction> _transactionRepository;

    public AccountService(IRepository<Account> accountRepository, IRepository<Transaction> transactionRepository)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Account?> GetByIdAsync(Guid id, string userId)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        return account?.UserId == userId ? account : null;
    }

    public async Task<IEnumerable<Account>> GetAllAsync(string userId)
    {
        return await _accountRepository.FindAsync(a => a.UserId == userId && a.IsActive);
    }

    public async Task<Account> CreateAsync(string userId, string name, AccountType type, Currency currency, decimal initialBalance, string? description = null)
    {
        var account = new Account(userId, name, type, currency, initialBalance, description);
        await _accountRepository.AddAsync(account);
        await _accountRepository.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(Guid id, string userId, string name, string? description = null)
    {
        var account = await GetByIdAsync(id, userId)
            ?? throw new InvalidOperationException("Account not found.");
        account.UpdateName(name);
        account.UpdateDescription(description);
        _accountRepository.Update(account);
        await _accountRepository.SaveChangesAsync();
    }

    public async Task DeactivateAsync(Guid id, string userId)
    {
        var account = await GetByIdAsync(id, userId)
            ?? throw new InvalidOperationException("Account not found.");
        account.Deactivate();
        _accountRepository.Update(account);
        await _accountRepository.SaveChangesAsync();
    }

    public async Task<decimal> GetBalanceAsync(Guid accountId, string userId)
    {
        var account = await GetByIdAsync(accountId, userId)
            ?? throw new InvalidOperationException("Account not found.");

        var transactions = await _transactionRepository.FindAsync(
            t => t.AccountId == accountId && t.UserId == userId);

        var balance = account.InitialBalance;
        foreach (var t in transactions)
        {
            balance += t.Type == TransactionType.Income ? t.Amount : -t.Amount;
        }
        return balance;
    }
}

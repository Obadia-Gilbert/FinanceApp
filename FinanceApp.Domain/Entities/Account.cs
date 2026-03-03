using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Represents a financial account (bank account, wallet, credit card, etc.).
/// CurrentBalance is never stored — it's always computed as InitialBalance + SUM(Transactions).
/// </summary>
public class Account : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public AccountType Type { get; private set; }
    public Currency Currency { get; private set; }
    public decimal InitialBalance { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; }

    public ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    protected Account() { }

    public Account(string userId, string name, AccountType type, Currency currency, decimal initialBalance, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        UserId = userId;
        Name = name;
        Type = type;
        Currency = currency;
        InitialBalance = initialBalance;
        Description = description;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        Name = name;
    }

    public void UpdateDescription(string? description) => Description = description;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

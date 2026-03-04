using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// First-class income entry. Each income can be linked to a Transaction (Type=Income)
/// for ledger/account balance; create/update/delete keep the Transaction in sync.
/// </summary>
public class Income : BaseEntity
{
    public string UserId { get; private set; } = null!;
    /// <summary>Optional. When set, a Transaction is created so account balance updates.</summary>
    public Guid? AccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public DateTimeOffset IncomeDate { get; private set; }
    public string? Description { get; private set; }
    public string? Source { get; private set; }
    /// <summary>Linked ledger transaction (Type=Income). Null for legacy/import.</summary>
    public Guid? TransactionId { get; private set; }

    public Account? Account { get; private set; }
    public Category Category { get; private set; } = null!;
    public Transaction? Transaction { get; private set; }

    protected Income() { }

    public Income(
        string userId,
        Guid? accountId,
        Guid categoryId,
        decimal amount,
        Currency currency,
        DateTimeOffset incomeDate,
        string? description = null,
        string? source = null,
        Guid? transactionId = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        UserId = userId;
        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        Currency = currency;
        IncomeDate = incomeDate;
        Description = description;
        Source = source;
        TransactionId = transactionId;
    }

    public void SetTransactionId(Guid transactionId) => TransactionId = transactionId;

    public void UpdateAmount(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        Amount = amount;
    }

    public void UpdateIncomeDate(DateTimeOffset date) => IncomeDate = date;
    public void UpdateCategory(Guid categoryId) => CategoryId = categoryId;
    public void UpdateDescription(string? description) => Description = description;
    public void UpdateSource(string? source) => Source = source;
    public void UpdateAccount(Guid? accountId) => AccountId = accountId;
}

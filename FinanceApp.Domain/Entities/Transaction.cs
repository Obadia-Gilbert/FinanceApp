using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Core ledger entity. Amount is always positive; Type determines direction.
/// Transfers create TWO Transaction records linked by TransferGroupId.
/// </summary>
public class Transaction : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public DateTimeOffset Date { get; private set; }
    public string? Note { get; private set; }
    public Guid? TransferGroupId { get; private set; }
    public bool IsRecurring { get; private set; }

    // Navigation properties
    public Account Account { get; private set; } = null!;
    public Category? Category { get; private set; }

    protected Transaction() { }

    public Transaction(
        string userId,
        Guid accountId,
        TransactionType type,
        decimal amount,
        Currency currency,
        DateTimeOffset date,
        Guid? categoryId = null,
        string? note = null,
        Guid? transferGroupId = null,
        bool isRecurring = false)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        UserId = userId;
        AccountId = accountId;
        Type = type;
        Amount = amount;
        Currency = currency;
        Date = date;
        CategoryId = categoryId;
        Note = note;
        TransferGroupId = transferGroupId;
        IsRecurring = isRecurring;
    }

    public void UpdateAmount(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        Amount = amount;
    }

    public void UpdateNote(string? note) => Note = note;
    public void UpdateDate(DateTimeOffset date) => Date = date;
    public void UpdateCategory(Guid? categoryId) => CategoryId = categoryId;
}

using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Expense : BaseEntity
{
    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; }

    public string UserId { get; private set; } = null!;

    /// <summary>Optional. When set, a Transaction is created so account balance updates.</summary>
    public Guid? AccountId { get; private set; }

    public DateTimeOffset ExpenseDate { get; private set; }

    public string? Description { get; private set; }

    public Guid CategoryId { get; private set; }

    public string? ReceiptPath { get; private set; }

    /// <summary>Set when this expense was synced to the ledger (AccountId was provided).</summary>
    public Guid? TransactionId { get; private set; }

    public Category Category { get; private set; } = null!;
    public Account? Account { get; private set; }

    // ✅ Parameterless constructor required by EF Core & MVC model binding
    protected Expense() { }

    public Expense(
        decimal amount,
        Currency currency,
        DateTimeOffset expenseDate,
        Guid categoryId,
        string userId,
        Guid? accountId = null,
        string? description = null,
        string? receiptPath = null,
        Guid? transactionId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        Amount = amount;
        Currency = currency;
        ExpenseDate = expenseDate;
        CategoryId = categoryId;
        UserId = userId;
        AccountId = accountId;
        Description = description;
        ReceiptPath = receiptPath;
        TransactionId = transactionId;
    }

    public void SetTransactionId(Guid transactionId) => TransactionId = transactionId;
    public void UpdateAccount(Guid? accountId) => AccountId = accountId;

    // 🔹 Optional update methods
    public void UpdateDescription(string description)
    {
        Description = description;
    }

    public void UpdateReceipt(string receiptPath)
    {
        ReceiptPath = receiptPath;
    }

    public void UpdateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
        Amount = amount;
    }

    public void UpdateCurrency(Currency currency)
    {
        Currency = currency;
    }

    public void UpdateExpenseDate(DateTimeOffset date)
    {
        ExpenseDate = date;
    }

    public void UpdateCategory(Guid categoryId)
    {
        CategoryId = categoryId;
    }
    
}
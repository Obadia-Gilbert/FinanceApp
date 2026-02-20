using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Expense : BaseEntity
{
    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; }

    public string UserId { get; private set; } = null!;

    public DateTimeOffset ExpenseDate { get; private set; }

    public string? Description { get; private set; }

    public Guid CategoryId { get; private set; }

    public string? ReceiptPath { get; private set; }

    //public Guid UserId { get; private set; }

    // 🔹 Navigation property to Category
    public Category Category { get; private set; } = null!;

    // ✅ Parameterless constructor required by EF Core & MVC model binding
    protected Expense() { }

    // 🔹 Main constructor for creating new Expense
    public Expense(
        decimal amount,
        Currency currency,
        DateTimeOffset expenseDate,
        Guid categoryId,
        string userId,
        string? description = null,
        string? receiptPath = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        Amount = amount;
        Currency = currency;
        ExpenseDate = expenseDate;
        CategoryId = categoryId;
        UserId = userId;
        Description = description;
        ReceiptPath = receiptPath;
    }

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
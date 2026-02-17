using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Expense : BaseEntity
{
    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; }

    public DateTimeOffset ExpenseDate { get; private set; }

    public string? Description { get; private set; }

    public Guid CategoryId { get; private set; }

    public string? ReceiptPath { get; private set; }

    public Guid UserId { get; private set; }

    // Navigation property to Category
    public Category Category { get; private set; } = null!;

    // Parameterless constructor for EF Core
    private Expense() { }

    // Main constructor
    public Expense(
        decimal amount,
        Currency currency,
        DateTimeOffset expenseDate,
        Guid categoryId,
        Guid userId,
        string? description = null,
        string? receiptPath = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        Amount = amount;
        Currency = currency;
        ExpenseDate = expenseDate;
        CategoryId = categoryId;
        UserId = userId;
        Description = description;
        ReceiptPath = receiptPath;
    }

    // Update methods
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
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        Amount = amount;
    }

    public void UpdateCurrency(Currency currency)
    {
        Currency = currency;
    }

    public void UpdateExpenseDate(DateTimeOffset expenseDate)
    {
        ExpenseDate = expenseDate;
    }
}
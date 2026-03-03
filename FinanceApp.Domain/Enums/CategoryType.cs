namespace FinanceApp.Domain.Enums;

/// <summary>
/// Category usage: Expense-only, Income-only, or both (e.g. "Transfer").
/// Used to filter category dropdowns when creating expenses vs income transactions.
/// </summary>
public enum CategoryType
{
    Expense = 0,
    Income = 1,
    Both = 2
}

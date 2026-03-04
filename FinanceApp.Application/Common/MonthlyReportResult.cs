namespace FinanceApp.Application.Common;

/// <summary>Result of a monthly expenses/budget report for a user.</summary>
public class MonthlyReportResult
{
    public int Month { get; init; }
    public int Year { get; init; }
    public string MonthName { get; init; } = "";
    public decimal TotalSpent { get; init; }
    public decimal TotalIncome { get; init; }
    public decimal NetCashFlow => TotalIncome - TotalSpent;
    public string Currency { get; init; } = "";
    public decimal? GlobalBudgetAmount { get; init; }
    public decimal? GlobalBudgetSpent { get; init; }
    public decimal? GlobalBudgetRemaining { get; init; }
    public bool IsOverGlobalBudget { get; init; }
    public IReadOnlyList<CategoryReportLine> CategoryLines { get; init; } = [];
    public IReadOnlyList<ExpenseReportLine> TopExpenses { get; init; } = [];
}

public class CategoryReportLine
{
    public string CategoryName { get; init; } = "";
    public decimal Spent { get; init; }
    public decimal? BudgetAmount { get; init; }
    public decimal? Remaining { get; init; }
    public bool IsOverBudget { get; init; }
}

public class ExpenseReportLine
{
    public string Description { get; init; } = "";
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "";
    public DateTime Date { get; init; }
    public string CategoryName { get; init; } = "";
}

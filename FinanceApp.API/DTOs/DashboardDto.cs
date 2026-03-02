namespace FinanceApp.API.DTOs;

public record DashboardDto(
    decimal TotalSpend,
    string DisplayCurrency,
    int ExpenseCount,
    int CategoryCount,
    decimal ThisMonthSpend,
    decimal? BudgetAmount,
    string? BudgetCurrency,
    bool IsOverBudget,
    IReadOnlyList<ChartDataPoint> ChartData,
    IReadOnlyList<CategoryBudgetAlertDto> CategoryBudgetAlerts);

public record ChartDataPoint(string Date, decimal Amount);

public record CategoryBudgetAlertDto(string CategoryName, decimal Spent, decimal Budget, string Currency, bool IsOver);

namespace FinanceApp.Application.Common;

/// <summary>One day's total for chart/trend.</summary>
public record ExpenseTotalByDayDto(DateTime Date, decimal Sum);

/// <summary>Category total for breakdown (e.g. top 6).</summary>
public record CategoryTotalDto(Guid CategoryId, string? CategoryName, decimal Sum);

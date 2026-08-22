using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Common;

/// <summary>One day's total for chart/trend.</summary>
public record ExpenseTotalByDayDto(DateTime Date, decimal Sum);

/// <summary>
/// Category total for breakdown (e.g. top 6), for a single currency.
/// Totals are always grouped per currency — a category with spend in more than one
/// currency yields one row per currency, so callers convert rather than summing
/// raw amounts across currencies.
/// </summary>
public record CategoryTotalDto(Guid CategoryId, string? CategoryName, decimal Sum, Currency Currency);

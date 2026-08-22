using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Converts monetary amounts between currencies so totals, budgets and reports can be
/// compared even when the underlying records were entered in different currencies.
/// </summary>
public interface ICurrencyConversionService
{
    /// <summary>
    /// Converts an amount in the given currency to USD.
    /// </summary>
    decimal ConvertToUsd(decimal amount, Currency currency);

    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    decimal Convert(decimal amount, Currency from, Currency to);

    /// <summary>
    /// Sums per-currency totals into a single target currency.
    /// Use this instead of looking up a single currency key — a plain lookup treats
    /// "no spend recorded in exactly this currency" as zero and silently ignores the rest.
    /// </summary>
    decimal SumInCurrency(IEnumerable<KeyValuePair<Currency, decimal>> totalsByCurrency, Currency target);

    /// <summary>
    /// Sums one category's per-currency spend into a single target currency.
    /// </summary>
    decimal SumCategoryInCurrency(
        IEnumerable<KeyValuePair<(Guid CategoryId, Currency Currency), decimal>> spendByCategoryAndCurrency,
        Guid categoryId,
        Currency target);
}

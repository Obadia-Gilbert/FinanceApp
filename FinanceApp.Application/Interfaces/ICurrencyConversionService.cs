using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Converts amounts from various currencies to USD for consistent dashboard totals.
/// </summary>
public interface ICurrencyConversionService
{
    /// <summary>
    /// Converts an amount in the given currency to USD.
    /// </summary>
    decimal ConvertToUsd(decimal amount, Currency currency);
}

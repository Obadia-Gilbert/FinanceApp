using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Converts amounts between currencies using configurable fixed exchange rates
/// (1 unit of currency = X USD). Configure in appsettings.json under "ExchangeRates"
/// (e.g. "TZS": 0.00038). Shared by Web and API so budget/dashboard totals agree
/// regardless of which client a user's expenses were recorded from.
/// </summary>
public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly IReadOnlyDictionary<Currency, decimal> _ratesToUsd;

    public CurrencyConversionService(IConfiguration configuration)
    {
        var section = configuration.GetSection("ExchangeRates");
        var dict = new Dictionary<Currency, decimal>();

        foreach (Currency currency in Enum.GetValues(typeof(Currency)))
        {
            var key = currency.ToString();
            var value = section.GetValue<decimal?>(key);
            dict[currency] = value ?? GetDefaultRateToUsd(currency);
        }

        _ratesToUsd = dict;
    }

    public decimal ConvertToUsd(decimal amount, Currency currency)
    {
        if (amount <= 0) return 0;
        return Math.Round(amount * _ratesToUsd[currency], 2, MidpointRounding.AwayFromZero);
    }

    public decimal Convert(decimal amount, Currency from, Currency to)
    {
        if (amount == 0) return 0;
        if (from == to) return amount;

        var usd = amount * _ratesToUsd[from];
        var converted = usd / _ratesToUsd[to];
        return Math.Round(converted, 2, MidpointRounding.AwayFromZero);
    }

    public decimal SumInCurrency(IEnumerable<KeyValuePair<Currency, decimal>> totalsByCurrency, Currency target)
    {
        if (totalsByCurrency is null) return 0;
        return totalsByCurrency.Sum(kv => Convert(kv.Value, kv.Key, target));
    }

    public decimal SumCategoryInCurrency(
        IEnumerable<KeyValuePair<(Guid CategoryId, Currency Currency), decimal>> spendByCategoryAndCurrency,
        Guid categoryId,
        Currency target)
    {
        if (spendByCategoryAndCurrency is null) return 0;
        return spendByCategoryAndCurrency
            .Where(kv => kv.Key.CategoryId == categoryId)
            .Sum(kv => Convert(kv.Value, kv.Key.Currency, target));
    }

    /// <summary>
    /// Fallback rates when not configured. 1 unit of currency = X USD. Update as needed.
    /// </summary>
    private static decimal GetDefaultRateToUsd(Currency currency)
    {
        return currency switch
        {
            Currency.USD => 1m,
            Currency.EUR => 1.08m,
            Currency.GBP => 1.27m,
            Currency.JPY => 0.0067m,
            Currency.AUD => 0.65m,
            Currency.CAD => 0.74m,
            Currency.CHF => 1.12m,
            Currency.TZS => 0.00038m,   // ~1 USD = 2630 TZS
            Currency.UGX => 0.00027m,
            Currency.KES => 0.0077m,
            Currency.RWF => 0.00078m,
            Currency.ZAR => 0.055m,
            Currency.CNY => 0.14m,
            Currency.INR => 0.012m,
            Currency.BRL => 0.20m,
            Currency.MXN => 0.058m,
            _ => 1m
        };
    }
}

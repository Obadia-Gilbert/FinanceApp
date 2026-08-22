using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Converts amounts between currencies using rates resolved through <see cref="IExchangeRateStore"/>
/// (live-fetched, falling back to a configured or hardcoded rate). Shared by Web and API so
/// budget/dashboard totals agree regardless of which client a user's expenses were recorded from.
/// </summary>
public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly IExchangeRateStore _rateStore;

    public CurrencyConversionService(IExchangeRateStore rateStore)
    {
        _rateStore = rateStore;
    }

    public decimal ConvertToUsd(decimal amount, Currency currency)
    {
        if (amount <= 0) return 0;
        return Math.Round(amount * _rateStore.GetRateToUsd(currency), 2, MidpointRounding.AwayFromZero);
    }

    public decimal Convert(decimal amount, Currency from, Currency to)
    {
        if (amount == 0) return 0;
        if (from == to) return amount;

        var usd = amount * _rateStore.GetRateToUsd(from);
        var converted = usd / _rateStore.GetRateToUsd(to);
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
}

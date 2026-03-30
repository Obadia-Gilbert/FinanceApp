using FinanceApp.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace FinanceApp.Infrastructure.Subscription;

/// <summary>
/// Maps store product identifiers to <see cref="SubscriptionPlan"/> from configuration.
/// </summary>
public sealed class SubscriptionProductMapper
{
    private readonly IReadOnlyDictionary<string, SubscriptionPlan> _apple;
    private readonly IReadOnlyDictionary<string, SubscriptionPlan> _google;

    public SubscriptionProductMapper(IConfiguration config)
    {
        _apple = LoadSection(config, "SubscriptionBilling:AppleProductIdToPlan");
        _google = LoadSection(config, "SubscriptionBilling:GoogleProductIdToPlan");
    }

    private static Dictionary<string, SubscriptionPlan> LoadSection(IConfiguration config, string path)
    {
        var section = config.GetSection(path);
        var dict = new Dictionary<string, SubscriptionPlan>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in section.GetChildren())
        {
            var key = child.Key;
            var val = child.Value;
            if (string.IsNullOrEmpty(val)) continue;
            if (Enum.TryParse<SubscriptionPlan>(val, ignoreCase: true, out var plan) && plan != SubscriptionPlan.Free)
                dict[key] = plan;
        }
        return dict;
    }

    public bool TryMapApple(string productId, out SubscriptionPlan plan) =>
        _apple.TryGetValue(productId, out plan);

    public bool TryMapGoogle(string productId, out SubscriptionPlan plan) =>
        _google.TryGetValue(productId, out plan);
}

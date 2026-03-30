namespace FinanceApp.Domain.Enums;

/// <summary>
/// Where the current subscription entitlement was granted (store billing vs manual admin).
/// </summary>
public enum SubscriptionBillingSource
{
    None = 0,
    /// <summary>Assigned by an administrator (support / testing).</summary>
    AdminManual = 1,
    Apple = 2,
    Google = 3
}

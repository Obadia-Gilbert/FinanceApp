using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Audit trail for subscription grants from stores or admin (support, disputes).
/// </summary>
public class SubscriptionPurchaseRecord : BaseEntity
{
    public string UserId { get; set; } = null!;
    public SubscriptionBillingSource BillingSource { get; set; }
    /// <summary>Store product identifier (e.g. Apple product id or Google SKU).</summary>
    public string ProductId { get; set; } = null!;
    /// <summary>Stable external id (e.g. Apple originalTransactionId, Google order id).</summary>
    public string ExternalTransactionId { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    /// <summary>Optional short note (e.g. webhook type, admin email).</summary>
    public string? Notes { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

public sealed class VerifyGoogleSubscriptionRequest
{
    /// <summary>Play Billing subscription SKU (e.g. pro_monthly).</summary>
    [Required]
    public string SubscriptionId { get; set; } = "";

    /// <summary>Purchase token from the purchase result.</summary>
    [Required]
    public string PurchaseToken { get; set; } = "";
}

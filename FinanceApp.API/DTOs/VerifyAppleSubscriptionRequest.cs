using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

public sealed class VerifyAppleSubscriptionRequest
{
    /// <summary>StoreKit 2 signed transaction JWS from the client after a successful purchase.</summary>
    [Required]
    public string SignedTransactionJws { get; set; } = "";
}

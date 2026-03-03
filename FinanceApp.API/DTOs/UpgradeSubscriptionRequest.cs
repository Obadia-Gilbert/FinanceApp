using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

public class UpgradeSubscriptionRequest
{
    [Required]
    public string Plan { get; set; } = ""; // "Pro" | "Premium"
}

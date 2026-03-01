using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class SubscriptionViewModel
{
    public SubscriptionPlan CurrentPlan { get; set; }
    public DateTimeOffset SubscriptionAssignedAt { get; set; }
}

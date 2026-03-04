namespace FinanceApp.Web.Models;

/// <summary>Single row for dashboard "Recent Transactions" (expense or income).</summary>
public class RecentActivityViewModel
{
    public DateTime Date { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public bool IsIncome { get; set; }
}

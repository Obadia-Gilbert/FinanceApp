namespace FinanceApp.Web.Models;

public class CategoryBudgetAlertViewModel
{
    public string CategoryName { get; set; } = "";
    public decimal Spent { get; set; }
    public decimal Budget { get; set; }
    public string Currency { get; set; } = "";
    public bool IsOver { get; set; }
}

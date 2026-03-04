using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Domain.Enums;

/// <summary>
/// Where this category can be used: expenses only, income only, or both (e.g. "Transfer").
/// Makes it explicit so users are not confused which list is for what.
/// </summary>
public enum CategoryType
{
    [Display(Name = "Expenses only")]
    Expense = 0,

    [Display(Name = "Income only")]
    Income = 1,

    [Display(Name = "Both (expenses & income)")]
    Both = 2
}

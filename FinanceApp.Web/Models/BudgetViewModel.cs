using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class BudgetViewModel
{
    [Range(1, 12)]
    public int Month { get; set; }

    [Range(2000, 2100)]
    public int Year { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Budget amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public Currency Currency { get; set; }
}

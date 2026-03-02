using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class CategoryBudgetItemViewModel
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public decimal Spent { get; set; }
    public bool IsOver { get; set; }
    public bool IsWarning { get; set; }
}

public class CategoryBudgetViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public string? CategoryName { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [Range(2000, 2100)]
    public int Year { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Budget amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public Currency Currency { get; set; }
}

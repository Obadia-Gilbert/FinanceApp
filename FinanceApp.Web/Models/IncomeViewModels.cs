using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class IncomeListViewModel
{
    public Guid Id { get; set; }
    public Guid? AccountId { get; set; }
    public string AccountName { get; set; } = "";
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public DateTime IncomeDate { get; set; }
    public string? Description { get; set; }
    public string? Source { get; set; }
}

public class IncomeCreateViewModel
{
    /// <summary>Optional. When set, a transaction is created so account balance updates.</summary>
    public Guid? AccountId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    public DateTime IncomeDate { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(200)]
    [Display(Name = "Source (e.g. employer, client)")]
    public string? Source { get; set; }

    [Display(Name = "Supporting document (optional)")]
    public IFormFile? SupportingFile { get; set; }
}

public class IncomeEditViewModel
{
    public Guid Id { get; set; }

    public Guid? AccountId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    public DateTime IncomeDate { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Source { get; set; }
}

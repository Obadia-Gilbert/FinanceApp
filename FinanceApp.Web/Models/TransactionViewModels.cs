using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class TransactionViewModel
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public DateTimeOffset Date { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Note { get; set; }
    public Guid? TransferGroupId { get; set; }
    public bool IsRecurring { get; set; }
}

public class TransactionCreateViewModel
{
    [Required]
    [Display(Name = "Account")]
    public Guid AccountId { get; set; }

    [Required]
    public TransactionType Type { get; set; } = TransactionType.Expense;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Display(Name = "Category")]
    public Guid? CategoryId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [Display(Name = "Recurring")]
    public bool IsRecurring { get; set; }
}

public class TransactionTransferViewModel
{
    [Required]
    [Display(Name = "From Account")]
    public Guid FromAccountId { get; set; }

    [Required]
    [Display(Name = "To Account")]
    public Guid ToAccountId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class TransactionEditViewModel
{
    public Guid Id { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Display(Name = "Category")]
    public Guid? CategoryId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    // read-only display
    public TransactionType Type { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public Currency Currency { get; set; }
}

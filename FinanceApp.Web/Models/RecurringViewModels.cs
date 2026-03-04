using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class RecurringTemplateListViewModel
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = "";
    public string? CategoryName { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public int Interval { get; set; }
    public DateTime NextRunDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Note { get; set; }
}

public class RecurringTemplateCreateViewModel
{
    [Required]
    public Guid AccountId { get; set; }

    public Guid? CategoryId { get; set; }

    [Required]
    public TransactionType Type { get; set; } = TransactionType.Expense;

    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public Currency Currency { get; set; } = Currency.TZS;

    [Required]
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;

    [Range(1, 365)]
    public int Interval { get; set; } = 1;

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime? EndDate { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

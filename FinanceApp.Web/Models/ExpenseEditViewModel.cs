using System;
using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class ExpenseEditViewModel
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public Currency Currency { get; set; }

    public DateTimeOffset ExpenseDate { get; set; }

    public Guid CategoryId { get; set; }

    public string? Description { get; set; }

       // For uploading a file
    [Display(Name = "Receipt")]
    public IFormFile? ReceiptFile { get; set; }

    public string? ReceiptPath { get; set; }
}
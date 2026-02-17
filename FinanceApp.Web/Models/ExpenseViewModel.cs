using System;
using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models
{
    public class ExpenseViewModel
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public Currency Currency { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTimeOffset ExpenseDate { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        public Guid CategoryId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        [Display(Name = "Receipt Path")]
        public string? ReceiptPath { get; set; }
    }
}
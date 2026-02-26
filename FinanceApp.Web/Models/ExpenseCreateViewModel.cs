using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FinanceApp.Domain.Enums;


namespace FinanceApp.Web.Models
{
    public class ExpenseCreateViewModel
    {
         [Required]
        public string? Description { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public Currency Currency { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        // For uploading a receipt
        [Display(Name = "Receipt")]
        public IFormFile? ReceiptFile { get; set; }

        // Optional: to store path after upload
        public string? ReceiptPath { get; set; }
    }
}
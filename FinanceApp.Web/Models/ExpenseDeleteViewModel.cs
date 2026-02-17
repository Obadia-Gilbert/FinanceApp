using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models
{
    public class ExpenseDeleteViewModel
    {
        public Guid Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }

        public string Description { get; set; } = string.Empty;

        public Currency Currency { get; set; }

        public DateTimeOffset ExpenseDate { get; set; }
    }
}

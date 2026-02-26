using System;

namespace FinanceApp.Web.Models
{
    public class CategoryDeleteViewModel
    {
        public Guid Id { get; set; }  // To identify which category to delete

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
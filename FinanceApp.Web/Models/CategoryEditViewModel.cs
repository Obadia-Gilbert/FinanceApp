using System;
using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Web.Models
{
    public class CategoryEditViewModel
    {
        [Required]
        public Guid Id { get; set; }  // Needed to identify which category is being edited

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
        public string? Description { get; set; }
    }
}
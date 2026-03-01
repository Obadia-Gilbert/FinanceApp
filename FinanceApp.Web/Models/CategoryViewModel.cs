using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Web.Models
{
    public class CategoryViewModel
    {
        public Guid Id { get; set; } // Needed for Edit/Delete

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters")]
        public string? Description { get; set; }

        // new fields for icon and color display
        public string? Icon { get; set; }
        public string? BadgeColor { get; set; }
    }
}
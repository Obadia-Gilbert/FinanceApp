using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Web.Models
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters.")]
        public string? Icon { get; set; } = "shopping-cart"; // Default icon name

        [StringLength(7, ErrorMessage = "Badge color must be a valid hex color (e.g., #137fec).")]
        public string BadgeColor { get; set; } = "#137fec"; // Default blue color
    }
}
using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

public record CreateCategoryRequest(
    [Required][MinLength(1)] string Name,
    string? Description = null,
    string? Icon = null,
    string? BadgeColor = null);

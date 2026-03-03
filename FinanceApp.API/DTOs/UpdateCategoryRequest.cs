using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record UpdateCategoryRequest(
    [Required][MinLength(1)] string Name,
    CategoryType Type,
    string? Description = null,
    string? Icon = null,
    string? BadgeColor = null);

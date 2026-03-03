using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record CreateCategoryRequest(
    [Required][MinLength(1)] string Name,
    CategoryType Type = CategoryType.Expense,
    string? Description = null,
    string? Icon = null,
    string? BadgeColor = null);

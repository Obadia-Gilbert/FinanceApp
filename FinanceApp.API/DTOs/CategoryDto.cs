namespace FinanceApp.API.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    string BadgeColor);

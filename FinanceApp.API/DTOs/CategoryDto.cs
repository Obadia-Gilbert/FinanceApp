namespace FinanceApp.API.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    string? Icon,
    string BadgeColor);

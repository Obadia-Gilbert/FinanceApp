namespace FinanceApp.API.DTOs;

public record ProfileDto(
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Country,
    string? CountryCode,
    string? ProfileImagePath,
    string PreferredLanguage);

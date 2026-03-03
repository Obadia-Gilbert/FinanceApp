namespace FinanceApp.API.DTOs;

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    string Email,
    string FirstName,
    string LastName);

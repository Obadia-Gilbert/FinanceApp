namespace FinanceApp.API.DTOs;

public record LoginResponse(string Token, string Email, DateTime ExpiresAt);

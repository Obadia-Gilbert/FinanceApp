using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

public record RegisterRequest(
    [Required][MinLength(2)] string FirstName,
    [Required][MinLength(2)] string LastName,
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password);

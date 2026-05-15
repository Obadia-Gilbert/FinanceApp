using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

public record ForgotPasswordRequest(
    [Required][EmailAddress] string Email);

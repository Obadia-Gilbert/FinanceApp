using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

public record ResetPasswordRequest(
    [Required][EmailAddress] string Email,
    [Required] string Code,
    [Required][MinLength(6)] string NewPassword);

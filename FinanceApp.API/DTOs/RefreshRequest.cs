using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

public record RefreshRequest([Required] string RefreshToken);

using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record CreateExpenseRequest(
    [Range(0.01, double.MaxValue)] decimal Amount,
    Currency Currency,
    DateTime ExpenseDate,
    [Required] Guid CategoryId,
    string? Description = null);

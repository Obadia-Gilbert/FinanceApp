using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record SetBudgetRequest(
    [Range(1, 12)] int Month,
    [Range(2000, 2100)] int Year,
    [Range(0, double.MaxValue)] decimal Amount,
    Currency Currency);

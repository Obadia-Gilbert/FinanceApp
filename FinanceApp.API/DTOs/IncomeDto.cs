using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record IncomeDto(
    Guid Id,
    Guid? AccountId,
    string? AccountName,
    Guid CategoryId,
    string? CategoryName,
    decimal Amount,
    Currency Currency,
    DateTimeOffset IncomeDate,
    string? Description,
    string? Source,
    DateTimeOffset CreatedAt);

public record CreateIncomeRequest(
    Guid? AccountId,
    [Required] Guid CategoryId,
    [Range(0.01, double.MaxValue)] decimal Amount,
    Currency Currency,
    DateTime IncomeDate,
    string? Description = null,
    string? Source = null);

public record UpdateIncomeRequest(
    [Range(0.01, double.MaxValue)] decimal Amount,
    DateTime IncomeDate,
    Guid? AccountId,
    [Required] Guid CategoryId,
    string? Description = null,
    string? Source = null);

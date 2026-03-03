using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record TransactionDto(
    Guid Id,
    Guid AccountId,
    string? AccountName,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    DateTimeOffset Date,
    Guid? CategoryId,
    string? CategoryName,
    string? Note,
    Guid? TransferGroupId,
    bool IsRecurring,
    DateTimeOffset CreatedAt);

public record CreateTransactionRequest(
    [Required] Guid AccountId,
    TransactionType Type,
    [Range(0.01, double.MaxValue)] decimal Amount,
    Currency Currency,
    DateTime Date,
    Guid? CategoryId = null,
    string? Note = null,
    bool IsRecurring = false);

public record CreateTransferRequest(
    [Required] Guid FromAccountId,
    [Required] Guid ToAccountId,
    [Range(0.01, double.MaxValue)] decimal Amount,
    Currency Currency,
    DateTime Date,
    string? Note = null);

public record UpdateTransactionRequest(
    [Range(0.01, double.MaxValue)] decimal Amount,
    DateTime Date,
    Guid? CategoryId = null,
    string? Note = null);

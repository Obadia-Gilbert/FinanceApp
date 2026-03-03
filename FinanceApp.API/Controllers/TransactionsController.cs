using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? accountId = null,
        [FromQuery] TransactionType? type = null)
    {
        if (UserId == null) return Unauthorized();

        var paged = await _transactionService.GetPagedAsync(
            UserId, pageNumber, pageSize,
            filter: t =>
                (accountId == null || t.AccountId == accountId) &&
                (type == null || t.Type == type));

        return Ok(new PagedResultDto<TransactionDto>(
            paged.Items.Select(MapToDto).ToList(),
            paged.TotalItems, pageNumber, pageSize));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var t = await _transactionService.GetByIdAsync(id, UserId);
        if (t == null) return NotFound();
        return Ok(MapToDto(t));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        if (UserId == null) return Unauthorized();
        if (request.Type == TransactionType.Transfer)
            return BadRequest(new { message = "Use POST /api/transactions/transfer for transfers." });

        var t = await _transactionService.CreateAsync(
            UserId, request.AccountId, request.Type, request.Amount, request.Currency,
            new DateTimeOffset(request.Date), request.CategoryId, request.Note, request.IsRecurring);

        return CreatedAtAction(nameof(GetTransaction), new { id = t.Id }, MapToDto(t));
    }

    [HttpPost("transfer")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferRequest request)
    {
        if (UserId == null) return Unauthorized();
        if (request.FromAccountId == request.ToAccountId)
            return BadRequest(new { message = "Source and destination accounts must be different." });

        var (from, to) = await _transactionService.CreateTransferAsync(
            UserId, request.FromAccountId, request.ToAccountId,
            request.Amount, request.Currency, new DateTimeOffset(request.Date), request.Note);

        return StatusCode(StatusCodes.Status201Created, new { from = MapToDto(from), to = MapToDto(to) });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionRequest request)
    {
        if (UserId == null) return Unauthorized();

        await _transactionService.UpdateAsync(
            id, UserId, request.Amount, new DateTimeOffset(request.Date), request.CategoryId, request.Note);

        var t = await _transactionService.GetByIdAsync(id, UserId);
        if (t == null) return NotFound();
        return Ok(MapToDto(t));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var t = await _transactionService.GetByIdAsync(id, UserId);
        if (t == null) return NotFound();

        await _transactionService.DeleteAsync(id, UserId);
        return NoContent();
    }

    private static TransactionDto MapToDto(Transaction t) => new(
        t.Id, t.AccountId, t.Account?.Name,
        t.Type, t.Amount, t.Currency, t.Date,
        t.CategoryId, t.Category?.Name,
        t.Note, t.TransferGroupId, t.IsRecurring, t.CreatedAt);
}

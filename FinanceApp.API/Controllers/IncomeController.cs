using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncomeController : ControllerBase
{
    private readonly IIncomeService _incomeService;

    public IncomeController(IIncomeService incomeService)
    {
        _incomeService = incomeService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<IncomeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncomes(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? accountId = null)
    {
        if (UserId == null) return Unauthorized();

        System.Linq.Expressions.Expression<Func<Income, bool>>? filter = accountId.HasValue
            ? i => i.AccountId == accountId!.Value
            : null;
        var paged = await _incomeService.GetPagedAsync(UserId, pageNumber, pageSize, filter);
        var dtos = paged.Items.Select(MapToDto).ToList();
        return Ok(new PagedResultDto<IncomeDto>(dtos, paged.TotalItems, pageNumber, pageSize));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IncomeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncome(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var income = await _incomeService.GetByIdAsync(id, UserId);
        if (income == null) return NotFound();
        return Ok(MapToDto(income));
    }

    [HttpPost]
    [ProducesResponseType(typeof(IncomeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIncome([FromBody] CreateIncomeRequest request)
    {
        if (UserId == null) return Unauthorized();

        var accountId = request.AccountId is { } id && id != Guid.Empty ? id : (Guid?)null;
        var income = await _incomeService.CreateAsync(
            UserId,
            accountId,
            request.CategoryId,
            request.Amount,
            request.Currency,
            new DateTimeOffset(request.IncomeDate),
            request.Description,
            request.Source);

        return CreatedAtAction(nameof(GetIncome), new { id = income.Id }, MapToDto(income));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(IncomeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIncome(Guid id, [FromBody] UpdateIncomeRequest request)
    {
        if (UserId == null) return Unauthorized();

        var accountId = request.AccountId is { } aid && aid != Guid.Empty ? aid : (Guid?)null;
        await _incomeService.UpdateAsync(
            id,
            UserId,
            request.Amount,
            new DateTimeOffset(request.IncomeDate),
            accountId,
            request.CategoryId,
            request.Description,
            request.Source);

        var income = await _incomeService.GetByIdAsync(id, UserId);
        if (income == null) return NotFound();
        return Ok(MapToDto(income));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIncome(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var income = await _incomeService.GetByIdAsync(id, UserId);
        if (income == null) return NotFound();

        await _incomeService.DeleteAsync(id, UserId);
        return NoContent();
    }

    private static IncomeDto MapToDto(Income i) => new(
        i.Id,
        i.AccountId,
        i.Account?.Name,
        i.CategoryId,
        i.Category?.Name,
        i.Amount,
        i.Currency,
        i.IncomeDate,
        i.Description,
        i.Source,
        i.CreatedAt);
}

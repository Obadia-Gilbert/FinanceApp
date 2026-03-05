using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/recurring")]
[Authorize]
public class RecurringTemplatesController : ControllerBase
{
    private readonly IRecurringTemplateService _service;

    public RecurringTemplatesController(IRecurringTemplateService service)
    {
        _service = service;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<RecurringTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecurringTemplates(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (UserId == null) return Unauthorized();

        var paged = await _service.GetPagedAsync(UserId, pageNumber, pageSize);
        var dtos = paged.Items.Select(MapToDto).ToList();
        return Ok(new PagedResultDto<RecurringTemplateDto>(dtos, paged.TotalItems, pageNumber, pageSize));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RecurringTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecurringTemplate(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var template = await _service.GetByIdAsync(id, UserId);
        if (template == null) return NotFound();
        return Ok(MapToDto(template));
    }

    [HttpPost]
    [ProducesResponseType(typeof(RecurringTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRecurringTemplate([FromBody] CreateRecurringTemplateRequest request)
    {
        if (UserId == null) return Unauthorized();

        if (request.Type == (int)TransactionType.Transfer)
            return BadRequest("Recurring templates support Income or Expense only.");

        if (!DateTimeOffset.TryParse(request.StartDate, out var startDate))
            return BadRequest("Invalid StartDate.");

        DateTimeOffset? endDate = null;
        if (!string.IsNullOrWhiteSpace(request.EndDate) && DateTimeOffset.TryParse(request.EndDate, out var parsed))
            endDate = parsed;

        var template = await _service.CreateAsync(
            UserId,
            request.AccountId,
            (TransactionType)request.Type,
            request.Amount,
            (Currency)request.Currency,
            (RecurrenceFrequency)request.Frequency,
            startDate,
            request.CategoryId == Guid.Empty ? null : request.CategoryId,
            request.Note,
            request.Interval < 1 ? 1 : request.Interval,
            endDate);

        return CreatedAtAction(nameof(GetRecurringTemplate), new { id = template.Id }, MapToDto(template));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RecurringTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRecurringTemplate(Guid id, [FromBody] UpdateRecurringTemplateRequest request)
    {
        if (UserId == null) return Unauthorized();

        try
        {
            await _service.UpdateAsync(id, UserId, request.Amount, request.Note);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        var template = await _service.GetByIdAsync(id, UserId);
        return template == null ? NotFound() : Ok(MapToDto(template));
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateRecurringTemplate(Guid id)
    {
        if (UserId == null) return Unauthorized();

        try
        {
            await _service.DeactivateAsync(id, UserId);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRecurringTemplate(Guid id)
    {
        if (UserId == null) return Unauthorized();

        try
        {
            await _service.DeleteAsync(id, UserId);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private static RecurringTemplateDto MapToDto(RecurringTemplate r) => new(
        r.Id,
        r.AccountId,
        r.Account?.Name,
        r.CategoryId,
        r.Category?.Name,
        (int)r.Type,
        r.Amount,
        (int)r.Currency,
        (int)r.Frequency,
        r.Interval,
        r.StartDate,
        r.EndDate,
        r.NextRunDate,
        r.Note,
        r.IsActive);
}

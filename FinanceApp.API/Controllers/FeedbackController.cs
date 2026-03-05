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
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _service;

    public FeedbackController(IFeedbackService service)
    {
        _service = service;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<FeedbackDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFeedback(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (UserId == null) return Unauthorized();

        var paged = await _service.GetMyAsync(UserId, pageNumber, pageSize);
        var dtos = paged.Items.Select(MapToDto).ToList();
        return Ok(new PagedResultDto<FeedbackDto>(dtos, paged.TotalItems, pageNumber, pageSize));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeedback(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var feedback = await _service.GetByIdAsync(id, UserId);
        if (feedback == null) return NotFound();
        return Ok(MapToDto(feedback));
    }

    [HttpPost]
    [ProducesResponseType(typeof(FeedbackDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest request)
    {
        if (UserId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        if (request.Type < 0 || request.Type > 2)
            return BadRequest("Type must be 0 (Question), 1 (Suggestion), or 2 (Comment).");

        var feedback = await _service.CreateAsync(
            UserId,
            (FeedbackType)request.Type,
            request.Message.Trim(),
            string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim());

        return CreatedAtAction(nameof(GetFeedback), new { id = feedback.Id }, MapToDto(feedback));
    }

    private static FeedbackDto MapToDto(UserFeedback f) => new(
        f.Id,
        (int)f.Type,
        f.Subject,
        f.Message,
        f.CreatedAt);
}

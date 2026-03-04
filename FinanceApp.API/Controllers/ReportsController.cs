using System.Security.Claims;
using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMonthlyReportService _reportService;

    public ReportsController(IMonthlyReportService reportService)
    {
        _reportService = reportService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>GET /api/reports/monthly?year=2025&amp;month=3 — monthly expenses and budget summary.</summary>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(MonthlyReportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthlyReport([FromQuery] int year, [FromQuery] int month, [FromQuery] string? currency = null)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (month < 1 || month > 12) return BadRequest(new { error = "Month must be 1-12." });
        if (year < 2000 || year > 2100) return BadRequest(new { error = "Year must be 2000-2100." });

        var report = await _reportService.GetMonthlyReportAsync(userId, year, month, currency);
        return Ok(report);
    }
}

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
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ICategoryBudgetService _categoryBudgetService;

    public BudgetsController(IBudgetService budgetService, ICategoryBudgetService categoryBudgetService)
    {
        _budgetService = budgetService;
        _categoryBudgetService = categoryBudgetService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBudget([FromQuery] int month, [FromQuery] int year)
    {
        if (UserId == null) return Unauthorized();

        var budget = await _budgetService.GetBudgetForMonthAsync(UserId, month, year);
        if (budget == null)
            return NotFound();

        return Ok(MapToDto(budget));
    }

    [HttpPut]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetBudget([FromBody] SetBudgetRequest request)
    {
        if (UserId == null) return Unauthorized();

        var budget = await _budgetService.SetBudgetAsync(
            UserId,
            request.Month,
            request.Year,
            request.Amount,
            request.Currency);

        return Ok(MapToDto(budget));
    }

    [HttpGet("category")]
    [ProducesResponseType(typeof(IEnumerable<CategoryBudgetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategoryBudgets([FromQuery] int month, [FromQuery] int year)
    {
        if (UserId == null) return Unauthorized();

        var budgets = await _categoryBudgetService.GetForMonthAsync(UserId, month, year);
        var categorySpend = await _categoryBudgetService.GetCategorySpendForMonthAsync(UserId, month, year);

        var dtos = new List<CategoryBudgetDto>();
        foreach (var cb in budgets)
        {
            var key = (cb.CategoryId, cb.Currency);
            var spent = categorySpend.TryGetValue(key, out var s) ? s : 0;
            dtos.Add(new CategoryBudgetDto(
                cb.Id,
                cb.CategoryId,
                cb.Category?.Name,
                cb.Month,
                cb.Year,
                cb.Amount,
                cb.Currency,
                spent));
        }

        return Ok(dtos);
    }

    [HttpPut("category/{categoryId:guid}")]
    [ProducesResponseType(typeof(CategoryBudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetCategoryBudget(
        Guid categoryId,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromBody] SetBudgetRequest request)
    {
        if (UserId == null) return Unauthorized();

        var budget = await _categoryBudgetService.SetAsync(
            UserId,
            categoryId,
            month,
            year,
            request.Amount,
            request.Currency);

        var spent = await _categoryBudgetService.GetCategorySpendAsync(
            UserId, categoryId, month, year, budget.Currency);

        return Ok(new CategoryBudgetDto(
            budget.Id,
            budget.CategoryId,
            budget.Category?.Name,
            budget.Month,
            budget.Year,
            budget.Amount,
            budget.Currency,
            spent));
    }

    [HttpDelete("category/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCategoryBudget(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var deleted = await _categoryBudgetService.DeleteAsync(id, UserId);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    private static BudgetDto MapToDto(Budget b) => new(b.Id, b.Month, b.Year, b.Amount, b.Currency);
}

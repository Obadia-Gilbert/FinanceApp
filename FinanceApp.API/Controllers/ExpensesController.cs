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
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;

    public ExpensesController(IExpenseService expenseService, ICategoryService categoryService)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetExpenses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (UserId == null) return Unauthorized();

        var paged = await _expenseService.GetPagedExpensesAsync(
            pageNumber,
            pageSize,
            e => e.UserId == UserId,
            q => q.OrderByDescending(e => e.ExpenseDate));

        var dtos = paged.Items.Select(e => MapToDto(e)).ToList();
        return Ok(new PagedResultDto<ExpenseDto>(dtos, paged.TotalItems, pageNumber, pageSize));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetExpense(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null || expense.UserId != UserId)
            return NotFound();

        return Ok(MapToDto(expense));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        if (UserId == null) return Unauthorized();

        var expense = await _expenseService.CreateExpenseAsync(
            request.Amount,
            request.Currency,
            request.ExpenseDate,
            request.CategoryId,
            UserId,
            request.Description ?? "",
            null);

        return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, MapToDto(expense));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] UpdateExpenseRequest request)
    {
        if (UserId == null) return Unauthorized();

        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null || expense.UserId != UserId)
            return NotFound();

        expense.UpdateAmount(request.Amount);
        expense.UpdateCurrency(request.Currency);
        expense.UpdateExpenseDate(new DateTimeOffset(request.ExpenseDate));
        expense.UpdateCategory(request.CategoryId);
        expense.UpdateDescription(request.Description ?? "");

        await _expenseService.UpdateExpenseAsync(expense);
        return Ok(MapToDto(expense));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null || expense.UserId != UserId)
            return NotFound();

        await _expenseService.SoftDeleteExpenseAsync(id);
        return NoContent();
    }

    private static ExpenseDto MapToDto(Expense e) => new(
        e.Id,
        e.Amount,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.CategoryId,
        e.Category?.Name,
        e.ReceiptPath,
        e.CreatedAt);
}

public record PagedResultDto<T>(IReadOnlyList<T> Items, int TotalItems, int PageNumber, int PageSize);

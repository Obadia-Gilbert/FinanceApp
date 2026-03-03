using System.Security.Claims;
using ClosedXML.Excel;
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
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public ExpensesController(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _env = env;
        _config = config;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? categoryId = null)
    {
        if (UserId == null) return Unauthorized();

        var paged = await _expenseService.GetPagedExpensesAsync(
            pageNumber,
            pageSize,
            e => e.UserId == UserId && (!categoryId.HasValue || e.CategoryId == categoryId!.Value),
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

    /// <summary>GET api/expenses/{id}/receipt — stream receipt file (legacy ReceiptPath on Expense).</summary>
    [HttpGet("{id:guid}/receipt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetReceipt(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null || expense.UserId != UserId)
            return NotFound();
        if (string.IsNullOrEmpty(expense.ReceiptPath))
            return NotFound("No receipt for this expense.");

        var basePath = _config["Storage:ReceiptsBasePath"] ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(basePath, expense.ReceiptPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath))
            return NotFound("Receipt file not found.");

        var ext = Path.GetExtension(fullPath)?.ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
        var stream = System.IO.File.OpenRead(fullPath);
        return File(stream, contentType, Path.GetFileName(fullPath));
    }

    /// <summary>GET api/expenses/export/excel — download all user expenses as Excel.</summary>
    [HttpGet("export/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadExcel()
    {
        if (UserId == null) return Unauthorized();

        var paged = await _expenseService.GetPagedExpensesAsync(
            1, int.MaxValue,
            e => e.UserId == UserId,
            q => q.OrderByDescending(e => e.ExpenseDate));
        var expenses = paged.Items.ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Expenses");
        worksheet.Cell(1, 1).Value = "Description";
        worksheet.Cell(1, 2).Value = "Amount";
        worksheet.Cell(1, 3).Value = "Currency";
        worksheet.Cell(1, 4).Value = "Category";
        worksheet.Cell(1, 5).Value = "Expense Date";
        var row = 2;
        foreach (var e in expenses)
        {
            worksheet.Cell(row, 1).Value = e.Description;
            worksheet.Cell(row, 2).Value = e.Amount;
            worksheet.Cell(row, 3).Value = e.Currency.ToString();
            worksheet.Cell(row, 4).Value = e.Category?.Name;
            worksheet.Cell(row, 5).Value = e.ExpenseDate.ToString("yyyy-MM-dd");
            row++;
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Expenses.xlsx");
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

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
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategories()
    {
        if (UserId == null) return Unauthorized();

        var categories = await _categoryService.GetAllAsync(UserId);
        return Ok(categories.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var category = await _categoryService.GetByIdAsync(id, UserId);
        if (category == null)
            return NotFound();

        return Ok(MapToDto(category));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        if (UserId == null) return Unauthorized();

        var category = await _categoryService.CreateCategoryAsync(
            request.Name,
            UserId,
            request.Description,
            request.Icon,
            request.BadgeColor);

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, MapToDto(category));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        if (UserId == null) return Unauthorized();

        var existing = await _categoryService.GetByIdAsync(id, UserId);
        if (existing == null)
            return NotFound();

        await _categoryService.UpdateCategoryAsync(
            id,
            UserId,
            request.Name,
            request.Description,
            request.Icon,
            request.BadgeColor);

        var category = await _categoryService.GetByIdAsync(id, UserId);
        return Ok(MapToDto(category!));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var category = await _categoryService.GetByIdAsync(id, UserId);
        if (category == null)
            return NotFound();

        await _categoryService.DeleteCategoryAsync(id, UserId);
        return NoContent();
    }

    private static CategoryDto MapToDto(Category c) => new(
        c.Id,
        c.Name,
        c.Description,
        c.Icon,
        c.BadgeColor);
}

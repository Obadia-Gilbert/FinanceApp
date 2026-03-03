using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupportingDocumentsController : ControllerBase
{
    private readonly ISupportingDocumentService _documentService;

    public SupportingDocumentsController(ISupportingDocumentService documentService)
    {
        _documentService = documentService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>GET api/supportingdocuments?entityType=Expense&entityId={id}</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SupportingDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForEntity(
        [FromQuery] DocumentEntityType entityType,
        [FromQuery] Guid entityId)
    {
        if (UserId == null) return Unauthorized();

        var docs = await _documentService.GetForEntityAsync(entityType, entityId, UserId);
        return Ok(docs.Select(ToDto));
    }

    /// <summary>GET api/supportingdocuments/{id}</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupportingDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var doc = await _documentService.GetByIdAsync(id, UserId);
        return doc == null ? NotFound() : Ok(ToDto(doc));
    }

    /// <summary>GET api/supportingdocuments/{id}/download — streams the file (attachment).</summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var doc = await _documentService.GetByIdAsync(id, UserId);
        if (doc == null) return NotFound();

        var path = _documentService.GetFilePath(doc);
        if (!System.IO.File.Exists(path)) return NotFound("File not found on disk.");

        var stream = System.IO.File.OpenRead(path);
        return File(stream, doc.ContentType, doc.OriginalFileName);
    }

    /// <summary>GET api/supportingdocuments/{id}/preview — stream file with inline disposition (e.g. for browser preview).</summary>
    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var doc = await _documentService.GetByIdAsync(id, UserId);
        if (doc == null) return NotFound();

        var path = _documentService.GetFilePath(doc);
        if (!System.IO.File.Exists(path)) return NotFound("File not found on disk.");

        var stream = System.IO.File.OpenRead(path);
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{doc.OriginalFileName}\"";
        return File(stream, doc.ContentType);
    }

    /// <summary>POST api/supportingdocuments — multipart/form-data upload.</summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(SupportingDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromForm] DocumentEntityType entityType,
        [FromForm] Guid entityId,
        [FromForm] IFormFile file,
        [FromForm] string? label = null)
    {
        if (UserId == null) return Unauthorized();

        try
        {
            await using var stream = file.OpenReadStream();
            var doc = await _documentService.UploadAsync(
                UserId, entityType, entityId,
                file.FileName, file.ContentType, file.Length,
                stream, label);
            return CreatedAtAction(nameof(GetById), new { id = doc.Id }, ToDto(doc));
        }
        catch (ArgumentException ex)   { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>PATCH api/supportingdocuments/{id}/label</summary>
    [HttpPatch("{id:guid}/label")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLabel(Guid id, [FromBody] string? label)
    {
        if (UserId == null) return Unauthorized();

        try
        {
            await _documentService.UpdateLabelAsync(id, UserId, label);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>DELETE api/supportingdocuments/{id}</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (UserId == null) return Unauthorized();

        try
        {
            await _documentService.DeleteAsync(id, UserId);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    private static SupportingDocumentDto ToDto(Domain.Entities.SupportingDocument doc) =>
        new(doc.Id, doc.EntityType, doc.EntityId, doc.OriginalFileName,
            doc.ContentType, doc.FileSizeBytes, doc.Label, doc.CreatedAt);
}

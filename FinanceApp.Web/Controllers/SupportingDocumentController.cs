using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class SupportingDocumentController : Controller
{
    private readonly ISupportingDocumentService _documentService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SupportingDocumentController(
        ISupportingDocumentService documentService,
        UserManager<ApplicationUser> userManager)
    {
        _documentService = documentService;
        _userManager = userManager;
    }

    // POST: /SupportingDocument/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        DocumentEntityType entityType,
        Guid entityId,
        IFormFile file,
        string? label,
        string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        try
        {
            await using var stream = file.OpenReadStream();
            await _documentService.UploadAsync(
                userId, entityType, entityId,
                file.FileName, file.ContentType, file.Length,
                stream, label);

            if (isAjax)
                return Json(new { success = true, message = $"\u2018{file.FileName}\u2019 uploaded." });

            TempData["SuccessMessage"] = $"Document '{file.FileName}' uploaded successfully.";
        }
        catch (Exception ex)
        {
            if (isAjax)
                return Json(new { success = false, message = ex.Message });

            TempData["ErrorMessage"] = ex.Message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Expense");
    }

    // GET: /SupportingDocument/Download/{id}
    public async Task<IActionResult> Download(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var doc = await _documentService.GetByIdAsync(id, userId);
        if (doc == null) return NotFound();

        var path = _documentService.GetFilePath(doc);
        if (!System.IO.File.Exists(path)) return NotFound("File not found on disk.");

        var stream = System.IO.File.OpenRead(path);
        return File(stream, doc.ContentType, doc.OriginalFileName);
    }

    // GET: /SupportingDocument/Preview/{id} — inline view for images/PDFs
    public async Task<IActionResult> Preview(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var doc = await _documentService.GetByIdAsync(id, userId);
        if (doc == null) return NotFound();

        var path = _documentService.GetFilePath(doc);
        if (!System.IO.File.Exists(path)) return NotFound("File not found on disk.");

        var stream = System.IO.File.OpenRead(path);
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{doc.OriginalFileName}\"");
        return File(stream, doc.ContentType);
    }

    // POST: /SupportingDocument/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        try
        {
            await _documentService.DeleteAsync(id, userId);

            if (isAjax)
                return Json(new { success = true, message = "Document deleted." });

            TempData["SuccessMessage"] = "Document deleted.";
        }
        catch (KeyNotFoundException)
        {
            if (isAjax)
                return Json(new { success = false, message = "Document not found." });

            TempData["ErrorMessage"] = "Document not found.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Expense");
    }
}

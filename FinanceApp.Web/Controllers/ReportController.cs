using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Application.Common;
using FinanceApp.Infrastructure.Identity;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly IMonthlyReportService _reportService;
    private readonly ISharedReportService _sharedReportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportController(IMonthlyReportService reportService, ISharedReportService sharedReportService, UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _sharedReportService = sharedReportService;
        _userManager = userManager;
    }

    private string? UserId => _userManager.GetUserId(User);

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var now = DateTime.Now;
        var y = year ?? now.Year;
        var m = month ?? now.Month;
        if (m < 1 || m > 12) m = now.Month;
        if (y < 2000 || y > 2100) y = now.Year;

        var report = await _reportService.GetMonthlyReportAsync(userId, y, m);
        ViewBag.Report = report;
        ViewBag.SelectedYear = y;
        ViewBag.SelectedMonth = m;
        ViewBag.Years = Enumerable.Range(now.Year - 2, 5).ToList();
        return View();
    }

    /// <summary>Download report as HTML (opens in new tab; user can save).</summary>
    [HttpGet]
    public async Task<IActionResult> Download(int year, int month)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (month < 1 || month > 12) return BadRequest();
        var report = await _reportService.GetMonthlyReportAsync(userId, year, month);
        return View("ReportHtml", report);
    }

    /// <summary>Create a shareable link for the given month; returns URL in JSON or redirects with TempData.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShareLink(int year, int month, [FromQuery] int expiryDays = 7)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (month < 1 || month > 12) return BadRequest();
        if (expiryDays < 1 || expiryDays > 30) expiryDays = 7;
        var shared = await _sharedReportService.CreateAsync(userId, year, month, expiryDays);
        var url = Url.Action("Shared", "Report", new { token = shared.Token }, Request.Scheme);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, url, expiresAt = shared.ExpiresAt });
        TempData["ShareReportUrl"] = url;
        TempData["ShareReportExpires"] = shared.ExpiresAt.ToString("g", CultureInfo.CurrentUICulture);
        return RedirectToAction(nameof(Index), new { year, month });
    }

    /// <summary>Public view of a shared report (no auth required).</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Shared(string? token)
    {
        if (string.IsNullOrEmpty(token)) return NotFound();
        var shared = await _sharedReportService.GetByTokenAsync(token);
        if (shared == null) return NotFound();
        var report = await _reportService.GetMonthlyReportAsync(shared.UserId, shared.Year, shared.Month);
        return View("ReportHtml", report);
    }
}

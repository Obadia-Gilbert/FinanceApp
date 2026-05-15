using FinanceApp.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FinanceApp.Web.Controllers;

[AllowAnonymous]
public class LandingController : Controller
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LandingController(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    /// <summary>
    /// Public landing page at /. Authenticated users are redirected to the dashboard.
    /// </summary>
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');

        ViewData["Title"] = _localizer["Land_PageTitle"].Value;
        ViewData["MetaDescription"] = _localizer["Land_MetaDescription"].Value;
        ViewData["CanonicalUrl"] = baseUrl + "/";
        ViewData["OgImage"] = baseUrl + Url.Content("~/financeapp-logo.png");

        return View();
    }
}

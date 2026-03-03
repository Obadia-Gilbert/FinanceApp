using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Controllers;

[AllowAnonymous]
public class LandingController : Controller
{
    /// <summary>
    /// Public landing page at /. Authenticated users are redirected to the dashboard.
    /// </summary>
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');

        ViewData["Title"] = "FinanceApp – Personal Finance & Budget Tracker";
        ViewData["MetaDescription"] = "Track expenses, manage budgets, and get insights with FinanceApp. Free personal finance and budget tracker with categories, dashboards, and multi-currency support.";
        ViewData["CanonicalUrl"] = baseUrl + "/";
        ViewData["OgImage"] = baseUrl + Url.Content("~/financeapp-logo.png");

        return View();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Areas.Admin;

/// <summary>
/// Redirects /Admin/Profile/Edit to the root Profile/Edit (profile lives outside the Admin area).
/// </summary>
[Area("Admin")]
[Authorize]
public class ProfileController : Controller
{
    [HttpGet]
    [HttpPost]
    public IActionResult Edit()
    {
        return RedirectToAction("Edit", "Profile", new { area = "" });
    }
}

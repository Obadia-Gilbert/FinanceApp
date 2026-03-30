using FinanceApp.Infrastructure.Identity;
using FinanceApp.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Controllers;

[AllowAnonymous]
public sealed class LanguageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public LanguageController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Sets culture cookie and persists <see cref="ApplicationUser.PreferredLanguage"/> when signed in.</summary>
    [HttpGet]
    public async Task<IActionResult> Set(string culture, string? returnUrl = null)
    {
        culture = SupportedLanguages.Normalize(culture);
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax
            });

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.PreferredLanguage = culture;
                await _userManager.UpdateAsync(user);
            }
        }

        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            return RedirectToAction("Index", "Home");
        return LocalRedirect(returnUrl);
    }
}

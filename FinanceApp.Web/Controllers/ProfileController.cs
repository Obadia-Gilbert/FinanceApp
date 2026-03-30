using FinanceApp.Infrastructure.Identity;
using FinanceApp.Localization;
using FinanceApp.Web.Helpers;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var model = new ProfileEditViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Country = user.Country,
            CountryCode = user.CountryCode,
            CurrentProfileImagePath = user.ProfileImagePath,
            Email = user.Email,
            PreferredLanguage = SupportedLanguages.Normalize(user.PreferredLanguage)
        };
        ViewBag.Countries = new SelectList(
            CountryList.All.Select(c => new SelectListItem { Value = c.Code, Text = c.Name }),
            "Value",
            "Text",
            user.CountryCode);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (ModelState.IsValid)
        {
            user.FirstName = model.FirstName?.Trim();
            user.LastName = model.LastName?.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber?.Trim();
            user.CountryCode = string.IsNullOrWhiteSpace(model.CountryCode) ? null : model.CountryCode?.Trim();
            user.Country = CountryList.GetNameByCode(user.CountryCode) ?? model.Country?.Trim();
            user.PreferredLanguage = SupportedLanguages.Normalize(model.PreferredLanguage);

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                if (!string.IsNullOrEmpty(ext) && allowed.Contains(ext))
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await model.ProfileImage.CopyToAsync(stream);
                    user.ProfileImagePath = $"/uploads/profiles/{fileName}";
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                AppendCultureCookie(user.PreferredLanguage);
                TempData["ProfileUpdated"] = true;
                return RedirectToAction(nameof(Edit));
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }

        model.CurrentProfileImagePath = user.ProfileImagePath;
        model.Email = user.Email;
        model.PhoneNumber = user.PhoneNumber;
        model.Country = user.Country;
        model.CountryCode = user.CountryCode;
        ViewBag.Countries = new SelectList(
            CountryList.All.Select(c => new SelectListItem { Value = c.Code, Text = c.Name }),
            "Value",
            "Text",
            model.CountryCode);
        return View(model);
    }

    private void AppendCultureCookie(string culture)
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
    }
}

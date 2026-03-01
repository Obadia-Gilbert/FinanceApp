using FinanceApp.Infrastructure.Identity;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
            CurrentProfileImagePath = user.ProfileImagePath,
            Email = user.Email
        };
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
                TempData["ProfileUpdated"] = true;
                return RedirectToAction(nameof(Edit));
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }

        model.CurrentProfileImagePath = user.ProfileImagePath;
        model.Email = user.Email;
        return View(model);
    }
}

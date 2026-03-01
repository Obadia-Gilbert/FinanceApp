using System.Threading.Tasks;
using FinanceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.ViewComponents
{
    public class UserProfileViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserProfileViewComponent(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_signInManager.IsSignedIn(HttpContext.User))
                return View("Default", (DisplayName: (string?)null, ProfileImagePath: (string?)null));

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
                return View("Default", (DisplayName: (string?)null, ProfileImagePath: (string?)null));

            var displayName = !string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName)
                ? $"{user.FirstName?.Trim()} {user.LastName?.Trim()}".Trim()
                : user.Email ?? user.UserName;
            return View("Default", (DisplayName: displayName, ProfileImagePath: user.ProfileImagePath));
        }
    }
}

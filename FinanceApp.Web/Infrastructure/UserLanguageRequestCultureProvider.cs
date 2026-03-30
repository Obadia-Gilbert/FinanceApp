using FinanceApp.Infrastructure.Identity;
using FinanceApp.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;

namespace FinanceApp.Web.Infrastructure;

/// <summary>Uses <see cref="ApplicationUser.PreferredLanguage"/> when no culture cookie is present (e.g. new browser session).</summary>
public sealed class UserLanguageRequestCultureProvider : IRequestCultureProvider
{
    public async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.ContainsKey(Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName))
            return null;

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return null;

        var userManager = httpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null || string.IsNullOrWhiteSpace(user.PreferredLanguage))
            return null;

        var code = SupportedLanguages.Normalize(user.PreferredLanguage);
        return new ProviderCultureResult(code);
    }
}

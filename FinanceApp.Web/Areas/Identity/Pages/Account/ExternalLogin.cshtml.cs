// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly ICategoryService _categoryService;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender,
            ICategoryService categoryService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
            _categoryService = categoryService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ProviderDisplayName { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }
        
        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            // No external login row yet: resolve email (Google may use "email" JSON key until mapped in Program.cs).
            var emailFromProvider = ResolveEmailFromExternalLogin(info);
            if (!string.IsNullOrWhiteSpace(emailFromProvider))
            {
                var existingUser = await _userManager.FindByEmailAsync(emailFromProvider);
                if (existingUser != null)
                {
                    if (await _userManager.IsLockedOutAsync(existingUser))
                        return RedirectToPage("./Lockout");

                    var addLogin = await _userManager.AddLoginAsync(existingUser, info);
                    if (addLogin.Succeeded)
                    {
                        await _signInManager.SignInAsync(existingUser, isPersistent: false);
                        _logger.LogInformation(
                            "User {Email} signed in with {Provider}; external login was linked to existing account.",
                            emailFromProvider, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }

                    foreach (var err in addLogin.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    ErrorMessage = string.Join(" ", addLogin.Errors.Select(e => e.Description));
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }

                // First-time Google user: create account and sign in (no extra "complete registration" step).
                var created = await TryCreateAndSignInExternalUserAsync(info, emailFromProvider, returnUrl);
                if (created)
                    return LocalRedirect(returnUrl);
            }

            // No email from provider (rare): fall back to manual confirmation page.
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;
            if (!string.IsNullOrWhiteSpace(emailFromProvider))
                Input = new InputModel { Email = emailFromProvider };
            return Page();
        }

        /// <summary>Reads email from common claim types (Google userinfo + standard mappings).</summary>
        private static string ResolveEmailFromExternalLogin(ExternalLoginInfo info)
        {
            var p = info.Principal;
            if (p == null) return null;
            var e = p.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(e)) return e.Trim();
            e = p.FindFirstValue("email");
            if (!string.IsNullOrWhiteSpace(e)) return e.Trim();
            foreach (var claim in p.Claims)
            {
                if (string.Equals(claim.Type, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", StringComparison.OrdinalIgnoreCase))
                    return claim.Value?.Trim();
            }
            return null;
        }

        /// <summary>Creates local user + external login + default categories, then signs in.</summary>
        private async Task<bool> TryCreateAndSignInExternalUserAsync(ExternalLoginInfo info, string email, string returnUrl)
        {
            var user = CreateUser();
            user.EmailConfirmed = true;
            user.SubscriptionPlan = SubscriptionPlan.Free;
            user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;

            var given = info.Principal.FindFirstValue(ClaimTypes.GivenName);
            var family = info.Principal.FindFirstValue(ClaimTypes.Surname);
            if (string.IsNullOrWhiteSpace(given) && string.IsNullOrWhiteSpace(family))
            {
                var full = info.Principal.FindFirstValue(ClaimTypes.Name) ?? info.Principal.FindFirstValue("name");
                if (!string.IsNullOrWhiteSpace(full))
                {
                    var parts = full.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    given = parts.Length > 0 ? parts[0] : null;
                    family = parts.Length > 1 ? parts[1] : null;
                }
            }
            user.FirstName = string.IsNullOrWhiteSpace(given) ? null : given.Trim();
            user.LastName = string.IsNullOrWhiteSpace(family) ? null : family.Trim();

            await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, email, CancellationToken.None);

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var dup = createResult.Errors.Any(e =>
                    e.Code == "DuplicateUserName" || e.Code == "DuplicateEmail");
                if (dup)
                {
                    var other = await _userManager.FindByEmailAsync(email);
                    if (other != null && !await _userManager.IsLockedOutAsync(other))
                    {
                        var linkDup = await _userManager.AddLoginAsync(other, info);
                        if (linkDup.Succeeded)
                        {
                            await _signInManager.SignInAsync(other, isPersistent: false);
                            _logger.LogInformation("Linked {Provider} to existing {Email} after duplicate create race.", info.LoginProvider, email);
                            return true;
                        }
                    }
                }
                _logger.LogWarning("External auto-register failed for {Email}: {Errors}", email,
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return false;
            }

            var userId = await _userManager.GetUserIdAsync(user);
            await _userManager.AddToRoleAsync(user, "User");
            await _categoryService.AssignDefaultCategoriesToUserAsync(userId);

            var addLogin = await _userManager.AddLoginAsync(user, info);
            if (!addLogin.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                _logger.LogError("AddLogin failed after user create for {Email}", email);
                return false;
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("New user {Email} registered via {Provider}.", email, info.LoginProvider);
            return true;
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.SubscriptionPlan = SubscriptionPlan.Free;
                user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code },
                            protocol: Request.Scheme);

                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("./RegisterConfirmation", new { Email = Input.Email });
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}

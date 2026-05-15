// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using FinanceApp.Infrastructure.Email;
using FinanceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace FinanceApp.Web.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBrandedEmailSender _brandedEmailSender;
        private readonly LocalizedEmailTemplates _emailTemplates;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            IBrandedEmailSender brandedEmailSender,
            LocalizedEmailTemplates emailTemplates)
        {
            _userManager = userManager;
            _brandedEmailSender = brandedEmailSender;
            _emailTemplates = emailTemplates;
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Email link goes to ResetPassword with both `code` and `email` query
                // params so the form is pre-filled (mirrors the API forgot-password flow).
                var rawToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code = encodedToken, email = Input.Email },
                    protocol: Request.Scheme);

                var template = _emailTemplates.BuildResetPassword(
                    user.FirstName ?? user.Email ?? Input.Email,
                    callbackUrl);
                await _brandedEmailSender.SendAsync(Input.Email, template);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Email;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Localization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string GoogleLoginProvider = "Google";
    private const string FacebookLoginProvider = "Facebook";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICategoryService _categoryService;
    private readonly IConfiguration _config;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBrandedEmailSender _brandedEmailSender;
    private readonly LocalizedEmailTemplates _emailTemplates;
    private readonly EmailBrandingOptions _emailBranding;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenService refreshTokenService,
        ICategoryService categoryService,
        IConfiguration config,
        IStringLocalizer<SharedResource> localizer,
        IHttpClientFactory httpClientFactory,
        IBrandedEmailSender brandedEmailSender,
        LocalizedEmailTemplates emailTemplates,
        IOptions<EmailBrandingOptions> emailBrandingOptions,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
        _categoryService = categoryService;
        _config = config;
        _localizer = localizer;
        _httpClientFactory = httpClientFactory;
        _brandedEmailSender = brandedEmailSender;
        _emailTemplates = emailTemplates;
        _emailBranding = emailBrandingOptions.Value;
        _logger = logger;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = _localizer["Api_EmailRegistered"].Value });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true,
            SubscriptionPlan = SubscriptionPlan.Free,
            SubscriptionAssignedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, "User");
        await _categoryService.AssignDefaultCategoriesToUserAsync(user.Id);

        await TrySendWelcomeEmailAsync(user);

        var response = await BuildLoginResponseAsync(user);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Sends the branded welcome email when <c>EmailBranding:SendWelcomeEmail</c>
    /// is enabled (default true). Swallows all errors — registration must never
    /// fail because the email transport hiccupped.
    /// </summary>
    private async Task TrySendWelcomeEmailAsync(ApplicationUser user)
    {
        if (!_emailBranding.SendWelcomeEmail) return;
        if (string.IsNullOrWhiteSpace(user.Email)) return;

        try
        {
            var template = _emailTemplates.BuildWelcome(user.FirstName ?? user.Email!, _emailBranding.WebAppBaseUrl);
            await _brandedEmailSender.SendAsync(user.Email!, template);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Account created but welcome email could not be sent for {Email}.", user.Email);
        }
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = _localizer["Api_InvalidCredentials"].Value });

        var response = await BuildLoginResponseAsync(user);
        return Ok(response);
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var stored = await _refreshTokenService.GetByTokenAsync(request.RefreshToken);
        if (stored == null || !stored.IsActive)
            return Unauthorized(new { message = _localizer["Api_InvalidRefresh"].Value });

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user == null)
            return Unauthorized(new { message = _localizer["Api_UserNotFound"].Value });

        // Rotate: revoke old token, issue new pair
        await _refreshTokenService.RevokeAsync(request.RefreshToken);
        var response = await BuildLoginResponseAsync(user);
        return Ok(response);
    }

    // POST /api/auth/revoke  (logout — invalidate refresh token)
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request)
    {
        await _refreshTokenService.RevokeAsync(request.RefreshToken);
        return NoContent();
    }

    // POST /api/auth/forgot-password
    /// <summary>
    /// Sends a password-reset email if the address belongs to a confirmed account.
    /// Always returns 204 to avoid revealing whether an email is registered.
    /// The link in the email points to the FinanceApp.Web "ResetPassword" page,
    /// which uses the same Identity token machinery and so works for both web
    /// and mobile users.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var email = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);

        // Don't reveal whether the user exists — same behaviour as FinanceApp.Web/Identity.
        if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
        {
            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var resetUrl = BuildPasswordResetUrl(email, encodedToken);

                var template = _emailTemplates.BuildResetPassword(
                    user.FirstName ?? user.Email ?? email,
                    resetUrl);
                await _brandedEmailSender.SendAsync(email, template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password-reset email to {Email}.", email);
                // Swallow — surfacing the error would leak information and the
                // standard UX is "if an account exists you'll receive a link".
            }
        }

        return NoContent();
    }

    // POST /api/auth/reset-password
    /// <summary>
    /// Completes the password reset using the email + base64url-encoded token
    /// generated by <c>forgot-password</c>. Returns 204 on success, 400 with
    /// errors on validation failure (or generic failure to avoid leaking
    /// account existence).
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        // Same generic response regardless of user existence to avoid disclosure.
        if (user is null)
            return BadRequest(new { message = _localizer["Api_ResetPasswordInvalidToken"].Value });

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
        }
        catch (FormatException)
        {
            return BadRequest(new { message = _localizer["Api_ResetPasswordInvalidToken"].Value });
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (result.Succeeded)
            return NoContent();

        // Identity reports both invalid token and weak password through Errors.
        // Map known token errors to a localized message; surface password
        // policy errors verbatim.
        var tokenInvalid = result.Errors.Any(e =>
            e.Code is "InvalidToken" or "InvalidPasswordResetToken");
        if (tokenInvalid)
            return BadRequest(new { message = _localizer["Api_ResetPasswordInvalidToken"].Value });

        return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
    }

    /// <summary>Mobile OAuth: validates Google ID token or Facebook access token (same config keys as FinanceApp.Web: Authentication:Google / Authentication:Facebook).</summary>
    [HttpPost("external")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        var provider = request.Provider?.Trim().ToLowerInvariant();
        if (provider is not ("google" or "facebook"))
            return BadRequest(new { message = _localizer["Api_OAuthUnsupportedProvider"].Value });

        if (provider == "google")
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
                return BadRequest(new { message = _localizer["Api_OAuthInvalidToken"].Value });
            var audiences = GetGoogleIdTokenAudiences();
            if (audiences.Count == 0)
                return BadRequest(new { message = _localizer["Api_OAuthNotConfigured"].Value });

            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = audiences };
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
                var email = payload.Email?.Trim();
                if (string.IsNullOrEmpty(email))
                    return BadRequest(new { message = _localizer["Api_OAuthEmailRequired"].Value });
                if (!payload.EmailVerified)
                    return Unauthorized(new { message = _localizer["Api_OAuthInvalidToken"].Value });

                var first = payload.GivenName;
                var last = payload.FamilyName;
                var sub = payload.Subject;
                if (string.IsNullOrEmpty(sub))
                    return Unauthorized(new { message = _localizer["Api_OAuthInvalidToken"].Value });

                return await ExternalSignInAsync(email, first, last, GoogleLoginProvider, sub, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return Unauthorized(new { message = _localizer["Api_OAuthInvalidToken"].Value });
            }
        }

        // facebook
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            return BadRequest(new { message = _localizer["Api_OAuthInvalidToken"].Value });
        var fbAppId = _config["Authentication:Facebook:AppId"];
        if (string.IsNullOrWhiteSpace(fbAppId))
            return BadRequest(new { message = _localizer["Api_OAuthNotConfigured"].Value });

        if (!await FacebookDebugTokenValidAsync(request.AccessToken, cancellationToken))
            return Unauthorized(new { message = _localizer["Api_OAuthInvalidToken"].Value });

        var client = _httpClientFactory.CreateClient();
        var meUrl =
            $"https://graph.facebook.com/v21.0/me?fields=id,name,email,first_name,last_name&access_token={Uri.EscapeDataString(request.AccessToken)}";
        FacebookMeResponse? me;
        try
        {
            me = await client.GetFromJsonAsync<FacebookMeResponse>(meUrl, cancellationToken);
        }
        catch
        {
            return Unauthorized(new { message = _localizer["Api_OAuthInvalidToken"].Value });
        }

        if (me?.Id is not { Length: > 0 })
            return Unauthorized(new { message = _localizer["Api_OAuthInvalidToken"].Value });
        var fbEmail = me.Email?.Trim();
        if (string.IsNullOrEmpty(fbEmail))
            return BadRequest(new { message = _localizer["Api_OAuthEmailRequired"].Value });

        var fbFirst = me.First_name;
        var fbLast = me.Last_name;
        if (string.IsNullOrWhiteSpace(fbFirst) && string.IsNullOrWhiteSpace(fbLast) && !string.IsNullOrWhiteSpace(me.Name))
        {
            var parts = me.Name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            fbFirst = parts.Length > 0 ? parts[0] : null;
            fbLast = parts.Length > 1 ? parts[1] : null;
        }

        return await ExternalSignInAsync(fbEmail, fbFirst, fbLast, FacebookLoginProvider, me.Id, cancellationToken);
    }

    // -------------------------
    // Helpers
    // -------------------------

    private async Task<LoginResponse> BuildLoginResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var jwt = GenerateJwt(user, roles);
        var refreshToken = await _refreshTokenService.CreateAsync(
            user.Id,
            expirationDays: int.TryParse(_config["Jwt:RefreshExpirationDays"], out var d) ? d : 30);

        return new LoginResponse(
            jwt.Token,
            jwt.ExpiresAt,
            refreshToken.Token,
            user.Email ?? "",
            user.FirstName ?? "",
            user.LastName ?? "");
    }

    private (string Token, DateTime ExpiresAt) GenerateJwt(ApplicationUser user, IList<string> roles)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
        var issuer = _config["Jwt:Issuer"] ?? "FinanceApp.API";
        var audience = _config["Jwt:Audience"] ?? "FinanceApp";
        var expMinutes = int.TryParse(_config["Jwt:ExpirationMinutes"], out var m) ? m : 30;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName ?? ""),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(expMinutes);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiresAt, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private string BuildPasswordResetUrl(string email, string encodedToken)
    {
        var baseUrl = (_config["PasswordReset:WebAppBaseUrl"] ?? string.Empty).Trim().TrimEnd('/');
        var path = (_config["PasswordReset:ResetPath"] ?? "/Identity/Account/ResetPassword").Trim();
        if (!path.StartsWith('/')) path = "/" + path;

        // Fall back to the current request host so dev environments without
        // explicit configuration still produce a clickable link.
        if (string.IsNullOrEmpty(baseUrl))
            baseUrl = $"{Request.Scheme}://{Request.Host}";

        var query = $"?area=Identity&code={Uri.EscapeDataString(encodedToken)}&email={Uri.EscapeDataString(email)}";
        return baseUrl + path + query;
    }

    private List<string> GetGoogleIdTokenAudiences()
    {
        var list = new List<string>();
        var main = _config["Authentication:Google:ClientId"];
        if (!string.IsNullOrWhiteSpace(main))
            list.Add(main.Trim());
        var extra = _config["Authentication:Google:IdTokenAudiences"];
        if (!string.IsNullOrWhiteSpace(extra))
        {
            list.AddRange(extra.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()));
        }

        return list.Distinct(StringComparer.Ordinal).ToList();
    }

    private async Task<bool> FacebookDebugTokenValidAsync(string userAccessToken, CancellationToken cancellationToken)
    {
        var appId = _config["Authentication:Facebook:AppId"]?.Trim();
        var appSecret = _config["Authentication:Facebook:AppSecret"]?.Trim();
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
            return true;

        var client = _httpClientFactory.CreateClient();
        var appAccess = $"{appId}|{appSecret}";
        var url =
            $"https://graph.facebook.com/v21.0/debug_token?input_token={Uri.EscapeDataString(userAccessToken)}&access_token={Uri.EscapeDataString(appAccess)}";
        try
        {
            var doc = await client.GetFromJsonAsync<FacebookDebugTokenResponse>(url, cancellationToken);
            return doc?.Data is { Is_valid: true } && string.Equals(doc.Data.App_id, appId, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private async Task<IActionResult> ExternalSignInAsync(
        string email,
        string? firstName,
        string? lastName,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            if (await _userManager.IsLockedOutAsync(user))
                return Unauthorized(new { message = _localizer["Api_InvalidCredentials"].Value });

            var logins = await _userManager.GetLoginsAsync(user);
            if (!logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey))
            {
                var add = await _userManager.AddLoginAsync(user, new UserLoginInfo(loginProvider, providerKey, loginProvider));
                if (!add.Succeeded)
                    return BadRequest(new { message = string.Join("; ", add.Errors.Select(e => e.Description)) });
            }

            return Ok(await BuildLoginResponseAsync(user));
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim(),
            EmailConfirmed = true,
            SubscriptionPlan = SubscriptionPlan.Free,
            SubscriptionAssignedAt = DateTimeOffset.UtcNow
        };
        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var dupEmail = createResult.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail");
            if (dupEmail)
            {
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null)
                    return await ExternalSignInAsync(email, firstName, lastName, loginProvider, providerKey, cancellationToken);
            }

            return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, "User");
        await _categoryService.AssignDefaultCategoriesToUserAsync(user.Id);
        var addLogin = await _userManager.AddLoginAsync(user, new UserLoginInfo(loginProvider, providerKey, loginProvider));
        if (!addLogin.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(new { message = string.Join("; ", addLogin.Errors.Select(e => e.Description)) });
        }

        return Ok(await BuildLoginResponseAsync(user));
    }

    private sealed class FacebookMeResponse
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("first_name")]
        public string? First_name { get; set; }
        [JsonPropertyName("last_name")]
        public string? Last_name { get; set; }
    }

    private sealed class FacebookDebugTokenResponse
    {
        [JsonPropertyName("data")]
        public FacebookDebugData? Data { get; set; }
    }

    private sealed class FacebookDebugData
    {
        [JsonPropertyName("app_id")]
        public string? App_id { get; set; }
        [JsonPropertyName("is_valid")]
        public bool Is_valid { get; set; }
    }
}

using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.API.Helpers;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountDeletionService _accountDeletionService;

    public ProfileController(UserManager<ApplicationUser> userManager, IAccountDeletionService accountDeletionService)
    {
        _userManager = userManager;
        _accountDeletionService = accountDeletionService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        if (UserId == null) return Unauthorized();
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();
        return Ok(new ProfileDto(
            user.FirstName, user.LastName, user.Email, user.PhoneNumber,
            user.Country, user.CountryCode, user.ProfileImagePath,
            SupportedLanguages.Normalize(user.PreferredLanguage),
            user.DailyReminderEnabled));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (UserId == null) return Unauthorized();
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();
        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber?.Trim();
        user.CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode?.Trim();
        user.Country = CountryHelper.GetNameByCode(user.CountryCode) ?? user.Country;
        if (request.PreferredLanguage != null)
            user.PreferredLanguage = SupportedLanguages.Normalize(request.PreferredLanguage);
        if (request.DailyReminderEnabled.HasValue)
            user.DailyReminderEnabled = request.DailyReminderEnabled.Value;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        return Ok(new ProfileDto(
            user.FirstName, user.LastName, user.Email, user.PhoneNumber,
            user.Country, user.CountryCode, user.ProfileImagePath,
            SupportedLanguages.Normalize(user.PreferredLanguage),
            user.DailyReminderEnabled));
    }

    [HttpGet("deletion-status")]
    [ProducesResponseType(typeof(AccountAuthStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDeletionStatus()
    {
        if (UserId == null) return Unauthorized();
        return Ok(new AccountAuthStatusDto(await _accountDeletionService.HasPasswordAsync(UserId)));
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        if (UserId == null) return Unauthorized();

        var auth = await _accountDeletionService.VerifyDeletionAuthorizationAsync(
            UserId, request.CurrentPassword, request.ConfirmationPhrase);
        if (!auth.Authorized)
            return BadRequest(new { errors = new[] { auth.ErrorMessage } });

        var result = await _accountDeletionService.DeleteAccountAsync(UserId);
        if (!result.Success)
            return BadRequest(new { errors = new[] { result.ErrorMessage } });

        return NoContent();
    }
}

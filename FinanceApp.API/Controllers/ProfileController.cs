using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.API.Helpers;
using FinanceApp.Infrastructure.Identity;
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

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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
            user.Country, user.CountryCode, user.ProfileImagePath));
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
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        return Ok(new ProfileDto(
            user.FirstName, user.LastName, user.Email, user.PhoneNumber,
            user.Country, user.CountryCode, user.ProfileImagePath));
    }
}

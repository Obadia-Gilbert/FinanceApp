using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICategoryService _categoryService;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenService refreshTokenService,
        ICategoryService categoryService,
        IConfiguration config)
    {
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
        _categoryService = categoryService;
        _config = config;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = "Email is already registered." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, "User");
        await _categoryService.AssignDefaultCategoriesToUserAsync(user.Id);

        var response = await BuildLoginResponseAsync(user);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password." });

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
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user == null)
            return Unauthorized(new { message = "User not found." });

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
}

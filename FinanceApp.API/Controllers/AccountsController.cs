using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts()
    {
        if (UserId == null) return Unauthorized();

        var accounts = await _accountService.GetAllAsync(UserId);
        var dtos = new List<AccountDto>();
        foreach (var a in accounts)
        {
            var balance = await _accountService.GetBalanceAsync(a.Id, UserId);
            dtos.Add(MapToDto(a, balance));
        }
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var account = await _accountService.GetByIdAsync(id, UserId);
        if (account == null) return NotFound();

        var balance = await _accountService.GetBalanceAsync(id, UserId);
        return Ok(MapToDto(account, balance));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        if (UserId == null) return Unauthorized();

        var account = await _accountService.CreateAsync(
            UserId, request.Name, request.Type, request.Currency,
            request.InitialBalance, request.Description);

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, MapToDto(account, account.InitialBalance));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest request)
    {
        if (UserId == null) return Unauthorized();

        await _accountService.UpdateAsync(id, UserId, request.Name, request.Description);
        var account = await _accountService.GetByIdAsync(id, UserId);
        if (account == null) return NotFound();

        var balance = await _accountService.GetBalanceAsync(id, UserId);
        return Ok(MapToDto(account, balance));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAccount(Guid id)
    {
        if (UserId == null) return Unauthorized();

        var account = await _accountService.GetByIdAsync(id, UserId);
        if (account == null) return NotFound();

        await _accountService.DeactivateAsync(id, UserId);
        return NoContent();
    }

    private static AccountDto MapToDto(Account a, decimal balance) => new(
        a.Id, a.Name, a.Type, a.Currency, a.InitialBalance, balance, a.Description, a.IsActive);
}

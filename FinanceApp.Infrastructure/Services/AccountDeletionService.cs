using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Purges a user's account: no business table has a real DB foreign key to AspNetUsers (each
/// just holds a plain UserId string), so UserManager.DeleteAsync alone would silently orphan
/// every row. This deletes owned rows in an FK-safe order (respecting the real FK constraints
/// between business tables configured in FinanceDbContext), then the Identity user itself,
/// inside one transaction, then removes the user's uploaded files from disk.
/// </summary>
public class AccountDeletionService : IAccountDeletionService
{
    private readonly FinanceDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<AccountDeletionService> _logger;

    public AccountDeletionService(
        FinanceDbContext context,
        UserManager<ApplicationUser> userManager,
        IFileStorage fileStorage,
        ILogger<AccountDeletionService> logger)
    {
        _context = context;
        _userManager = userManager;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<bool> HasPasswordAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null && await _userManager.HasPasswordAsync(user);
    }

    public async Task<AccountDeletionAuthResult> VerifyDeletionAuthorizationAsync(
        string userId, string? currentPassword, string? confirmationPhrase)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new AccountDeletionAuthResult(false, "Account not found.");

        if (await _userManager.HasPasswordAsync(user))
        {
            if (string.IsNullOrEmpty(currentPassword))
                return new AccountDeletionAuthResult(false, "Current password is required.");
            var passwordOk = await _userManager.CheckPasswordAsync(user, currentPassword);
            return passwordOk
                ? new AccountDeletionAuthResult(true, null)
                : new AccountDeletionAuthResult(false, "Incorrect password.");
        }

        var expected = user.Email ?? user.UserName;
        var confirmed = !string.IsNullOrWhiteSpace(confirmationPhrase)
                         && string.Equals(confirmationPhrase.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        return confirmed
            ? new AccountDeletionAuthResult(true, null)
            : new AccountDeletionAuthResult(false, "Confirmation does not match your account email.");
    }

    public async Task<AccountDeletionResult> DeleteAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new AccountDeletionResult(true, null); // already gone — idempotent

        var profileImagePath = user.ProfileImagePath;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Leaf/independent tables first, then tables that are Restrict-referenced by others,
            // so nothing is deleted while something still points at it via a Restrict FK.
            _context.SupportingDocuments.RemoveRange(_context.SupportingDocuments.Where(x => x.UserId == userId));
            _context.Notifications.RemoveRange(_context.Notifications.Where(x => x.UserId == userId));
            _context.SharedReports.RemoveRange(_context.SharedReports.Where(x => x.UserId == userId));
            _context.UserFeedbacks.RemoveRange(_context.UserFeedbacks.Where(x => x.UserId == userId));
            _context.SubscriptionPurchaseRecords.RemoveRange(_context.SubscriptionPurchaseRecords.Where(x => x.UserId == userId));
            _context.RefreshTokens.RemoveRange(_context.RefreshTokens.Where(x => x.UserId == userId));
            _context.CategoryBudgets.RemoveRange(_context.CategoryBudgets.Where(x => x.UserId == userId));
            _context.Expenses.RemoveRange(_context.Expenses.Where(x => x.UserId == userId));
            _context.Incomes.RemoveRange(_context.Incomes.Where(x => x.UserId == userId));
            _context.RecurringTemplates.RemoveRange(_context.RecurringTemplates.Where(x => x.UserId == userId));
            _context.Transactions.RemoveRange(_context.Transactions.Where(x => x.UserId == userId));
            _context.Budgets.RemoveRange(_context.Budgets.Where(x => x.UserId == userId));
            _context.Accounts.RemoveRange(_context.Accounts.Where(x => x.UserId == userId));
            _context.Categories.RemoveRange(_context.Categories.Where(x => x.UserId == userId));
            await _context.SaveChangesAsync();

            var identityResult = await _userManager.DeleteAsync(user);
            if (!identityResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return new AccountDeletionResult(false, string.Join("; ", identityResult.Errors.Select(e => e.Description)));
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Account deletion failed for user {UserId}, rolled back.", userId);
            return new AccountDeletionResult(false, "Account deletion failed. Nothing was changed.");
        }

        try
        {
            _fileStorage.DeleteDirectory(Path.Combine("documents", userId));

            if (!string.IsNullOrWhiteSpace(profileImagePath))
                _fileStorage.Delete(Path.Combine("profiles", Path.GetFileName(profileImagePath)));
        }
        catch (Exception ex)
        {
            // DB deletion already committed — log but don't fail the operation over leftover files.
            _logger.LogWarning(ex, "Account {UserId} was deleted but cleaning up uploaded files failed.", userId);
        }

        return new AccountDeletionResult(true, null);
    }
}

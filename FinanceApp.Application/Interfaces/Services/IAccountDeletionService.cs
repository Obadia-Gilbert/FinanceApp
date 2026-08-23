namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Self-service account deletion: verifying the user is authorized to delete their own
/// account, then permanently purging every row and uploaded file that belongs to them.
/// </summary>
public interface IAccountDeletionService
{
    /// <summary>True if the user has a local password set (drives which re-auth UI to show).</summary>
    Task<bool> HasPasswordAsync(string userId);

    /// <summary>
    /// Verifies the supplied credential authorizes deletion. Password accounts check
    /// <paramref name="currentPassword"/>; password-less (social-login-only) accounts check
    /// <paramref name="confirmationPhrase"/> against the user's own email address.
    /// </summary>
    Task<AccountDeletionAuthResult> VerifyDeletionAuthorizationAsync(
        string userId, string? currentPassword, string? confirmationPhrase);

    /// <summary>
    /// Permanently deletes the user's account: every owned row across every table, every
    /// uploaded file, and the Identity user itself. Wrapped in a single transaction — nothing
    /// is left partially purged. Idempotent: deleting an already-deleted userId succeeds.
    /// </summary>
    Task<AccountDeletionResult> DeleteAccountAsync(string userId);
}

public record AccountDeletionAuthResult(bool Authorized, string? ErrorMessage);

public record AccountDeletionResult(bool Success, string? ErrorMessage);

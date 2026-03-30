namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Verifies a StoreKit 2 signed transaction JWS from the client and extracts subscription fields.
/// </summary>
public interface IAppleStoreTransactionVerifier
{
    Task<AppleVerifiedTransaction?> VerifySignedTransactionAsync(string signedTransactionJws, CancellationToken cancellationToken = default);
}

public sealed record AppleVerifiedTransaction(
    string OriginalTransactionId,
    string ProductId,
    DateTimeOffset ExpiresAtUtc,
    bool AutoRenewStatus);

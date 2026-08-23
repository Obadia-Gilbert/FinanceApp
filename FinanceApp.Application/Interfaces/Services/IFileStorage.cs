namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Abstraction over where uploaded files (receipts, documents, profile photos) physically
/// live. Today's only implementation is local disk (<c>LocalFileStorage</c>), which is the
/// right call for a single always-on VPS with a persistent volume — see
/// FinanceApp.Documentations/GOING_LIVE.md. The point of the interface is that swapping to
/// object storage later (if the app outgrows one VPS) is a new implementation of this
/// interface, not a rewrite of every call site that touches a file.
/// </summary>
public interface IFileStorage
{
    /// <summary>Writes <paramref name="content"/> to <paramref name="relativePath"/>, creating any missing directories.</summary>
    Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens <paramref name="relativePath"/> for reading. Throws <see cref="FileNotFoundException"/> if it doesn't exist.</summary>
    Stream OpenRead(string relativePath);

    /// <summary>Deletes <paramref name="relativePath"/> if it exists; no-ops otherwise.</summary>
    void Delete(string relativePath);

    /// <summary>Deletes an entire directory (and everything under it) if it exists; no-ops otherwise.</summary>
    void DeleteDirectory(string relativePath);

    bool Exists(string relativePath);
}

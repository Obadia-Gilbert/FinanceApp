using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

public interface ISupportingDocumentService
{
    /// <summary>Returns all documents attached to a specific entity.</summary>
    Task<IEnumerable<SupportingDocument>> GetForEntityAsync(DocumentEntityType entityType, Guid entityId, string userId);

    /// <summary>Returns a single document by ID, scoped to the user.</summary>
    Task<SupportingDocument?> GetByIdAsync(Guid id, string userId);

    /// <summary>
    /// Saves the file stream to disk and creates the DB record.
    /// </summary>
    Task<SupportingDocument> UploadAsync(
        string userId,
        DocumentEntityType entityType,
        Guid entityId,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        Stream fileStream,
        string? label = null);

    /// <summary>Updates the optional label of a document.</summary>
    Task UpdateLabelAsync(Guid id, string userId, string? label);

    /// <summary>Deletes the DB record and the file from disk.</summary>
    Task DeleteAsync(Guid id, string userId);

    /// <summary>
    /// Opens the stored file for reading, or null if it's missing from storage (DB record
    /// exists but the file doesn't — e.g. manual disk intervention). Routed through
    /// <c>IFileStorage</c> rather than the caller touching the filesystem directly.
    /// </summary>
    Stream? OpenRead(SupportingDocument document);
}

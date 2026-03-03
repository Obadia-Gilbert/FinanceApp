using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// A file (image, PDF, etc.) attached to an Expense or Transaction as supporting evidence.
/// The file itself is stored in wwwroot/uploads/documents/{UserId}/.
/// </summary>
public class SupportingDocument : BaseEntity
{
    public string UserId { get; private set; } = null!;

    /// <summary>Which entity type this document is attached to.</summary>
    public DocumentEntityType EntityType { get; private set; }

    /// <summary>ID of the linked Expense or Transaction.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Original filename as uploaded by the user.</summary>
    public string OriginalFileName { get; private set; } = null!;

    /// <summary>Stored filename on disk (GUID-based to avoid collisions).</summary>
    public string StoredFileName { get; private set; } = null!;

    /// <summary>MIME content type, e.g. "image/jpeg" or "application/pdf".</summary>
    public string ContentType { get; private set; } = null!;

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>Optional user-provided label for this document.</summary>
    public string? Label { get; private set; }

    protected SupportingDocument() { }

    public SupportingDocument(
        string userId,
        DocumentEntityType entityType,
        Guid entityId,
        string originalFileName,
        string storedFileName,
        string contentType,
        long fileSizeBytes,
        string? label = null)
    {
        if (string.IsNullOrWhiteSpace(userId))         throw new ArgumentException("UserId is required.",         nameof(userId));
        if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("OriginalFileName is required.", nameof(originalFileName));
        if (string.IsNullOrWhiteSpace(storedFileName))  throw new ArgumentException("StoredFileName is required.",  nameof(storedFileName));
        if (string.IsNullOrWhiteSpace(contentType))     throw new ArgumentException("ContentType is required.",     nameof(contentType));

        UserId           = userId;
        EntityType       = entityType;
        EntityId         = entityId;
        OriginalFileName = originalFileName;
        StoredFileName   = storedFileName;
        ContentType      = contentType;
        FileSizeBytes    = fileSizeBytes;
        Label            = label;
    }

    public void UpdateLabel(string? label) => Label = label;
}

using FinanceApp.Domain.Enums;

namespace FinanceApp.API.DTOs;

public record SupportingDocumentDto(
    Guid Id,
    DocumentEntityType EntityType,
    Guid EntityId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string? Label,
    DateTimeOffset CreatedAt
);

using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Services;

public class SupportingDocumentService : ISupportingDocumentService
{
    private readonly IRepository<SupportingDocument> _repository;
    private readonly IFileStorage _fileStorage;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx"
    };

    public SupportingDocumentService(IRepository<SupportingDocument> repository, IFileStorage fileStorage)
    {
        _repository = repository;
        _fileStorage = fileStorage;
    }

    public async Task<IEnumerable<SupportingDocument>> GetForEntityAsync(
        DocumentEntityType entityType, Guid entityId, string userId)
    {
        return await _repository.FindAsync(
            d => d.UserId == userId && d.EntityType == entityType && d.EntityId == entityId);
    }

    public async Task<SupportingDocument?> GetByIdAsync(Guid id, string userId)
    {
        var doc = await _repository.GetByIdAsync(id);
        return doc?.UserId == userId ? doc : null;
    }

    public async Task<SupportingDocument> UploadAsync(
        string userId,
        DocumentEntityType entityType,
        Guid entityId,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        Stream fileStream,
        string? label = null)
    {
        if (fileStream == null || fileSizeBytes == 0)
            throw new ArgumentException("File stream is empty.");

        var ext = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        var storedFileName = $"{Guid.NewGuid()}{ext}";
        await _fileStorage.SaveAsync(RelativePath(userId, storedFileName), fileStream);

        var document = new SupportingDocument(
            userId, entityType, entityId,
            originalFileName, storedFileName,
            contentType, fileSizeBytes, label);

        await _repository.AddAsync(document);
        await _repository.SaveChangesAsync();
        return document;
    }

    public async Task UpdateLabelAsync(Guid id, string userId, string? label)
    {
        var doc = await GetByIdAsync(id, userId)
                  ?? throw new KeyNotFoundException($"Document {id} not found.");
        doc.UpdateLabel(label);
        _repository.Update(doc);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var doc = await GetByIdAsync(id, userId)
                  ?? throw new KeyNotFoundException($"Document {id} not found.");

        _fileStorage.Delete(RelativePath(doc.UserId, doc.StoredFileName));

        _repository.SoftDelete(doc);
        await _repository.SaveChangesAsync();
    }

    public Stream? OpenRead(SupportingDocument document)
    {
        var relativePath = RelativePath(document.UserId, document.StoredFileName);
        return _fileStorage.Exists(relativePath) ? _fileStorage.OpenRead(relativePath) : null;
    }

    private static string RelativePath(string userId, string storedFileName)
        => Path.Combine(userId, storedFileName);
}

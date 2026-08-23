using FinanceApp.Application.Interfaces.Services;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Local-disk implementation of <see cref="IFileStorage"/>, rooted at a single directory
/// (typically <c>wwwroot/uploads</c>). All paths passed in are relative to that root and
/// are resolved with <see cref="Path.Combine"/> — callers never touch <see cref="File"/> or
/// <see cref="Directory"/> directly.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root)
    {
        _root = root;
    }

    public async Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var dest = new FileStream(fullPath, FileMode.Create);
        await content.CopyToAsync(dest, cancellationToken);
    }

    public Stream OpenRead(string relativePath)
        => new FileStream(Resolve(relativePath), FileMode.Open, FileAccess.Read);

    public void Delete(string relativePath)
    {
        var fullPath = Resolve(relativePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    public void DeleteDirectory(string relativePath)
    {
        var fullPath = Resolve(relativePath);
        if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: true);
    }

    public bool Exists(string relativePath) => File.Exists(Resolve(relativePath));

    private string Resolve(string relativePath) => Path.Combine(_root, relativePath);
}

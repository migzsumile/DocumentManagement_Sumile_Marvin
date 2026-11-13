using System.Text;
using DocumentManagement.API.Options;
using Microsoft.Extensions.Options;

namespace DocumentManagement.API.Services;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IOptions<FileStorageOptions> options)
    {
        var configuredRoot = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            throw new InvalidOperationException("The file storage root path is required.");
        }

        _rootPath = Path.GetFullPath(configuredRoot);
        Directory.CreateDirectory(_rootPath);
    }

    public string BuildSafeStorageName(string originalFileName)
    {
        var safeFileName = string.IsNullOrWhiteSpace(originalFileName)
            ? "document"
            : Path.GetFileName(originalFileName);

        safeFileName = string.IsNullOrWhiteSpace(safeFileName)
            ? "document"
            : safeFileName;

        var extension = Path.GetExtension(safeFileName);
        if (string.IsNullOrWhiteSpace(extension) || extension.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            extension = ".bin";
        }

        var safeExtension = new string(extension
            .Where(ch => !Path.GetInvalidFileNameChars().Contains(ch))
            .ToArray());

        if (string.IsNullOrWhiteSpace(safeExtension))
        {
            safeExtension = ".bin";
        }

        return $"{Guid.NewGuid():N}{safeExtension}";
    }

    public async Task SaveAsync(Stream content, string storageName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var safeName = ValidateStorageName(storageName);
        var targetPath = ResolvePath(safeName);

        await using var output = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(output, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string storageName, CancellationToken cancellationToken = default)
    {
        var safeName = ValidateStorageName(storageName);
        var targetPath = ResolvePath(safeName);

        if (!File.Exists(targetPath))
        {
            throw new FileNotFoundException("The requested document file was not found.", targetPath);
        }

        var stream = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await Task.CompletedTask;
        return stream;
    }

    public Task<bool> ExistsAsync(string storageName, CancellationToken cancellationToken = default)
    {
        var safeName = ValidateStorageName(storageName);
        var targetPath = ResolvePath(safeName);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(targetPath));
    }

    public Task<bool> DeleteAsync(string storageName, CancellationToken cancellationToken = default)
    {
        var safeName = ValidateStorageName(storageName);
        var targetPath = ResolvePath(safeName);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(targetPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(targetPath);
        return Task.FromResult(true);
    }

    private string ResolvePath(string storageName)
    {
        var rootPath = Path.GetFullPath(_rootPath);
        var candidatePath = Path.GetFullPath(Path.Combine(rootPath, storageName));

        if (!candidatePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The storage path resolves outside the configured storage root.");
        }

        return candidatePath;
    }

    private static string ValidateStorageName(string storageName)
    {
        if (string.IsNullOrWhiteSpace(storageName))
        {
            throw new ArgumentException("The storage identifier is required.", nameof(storageName));
        }

        if (storageName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || storageName.Contains('/') || storageName.Contains('\\'))
        {
            throw new ArgumentException("The storage identifier contains invalid path characters.", nameof(storageName));
        }

        var fileName = Path.GetFileName(storageName);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != storageName || fileName.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("The storage identifier is malformed.", nameof(storageName));
        }

        return fileName;
    }
}

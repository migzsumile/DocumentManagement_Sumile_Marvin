using DocumentManagement.API.Context;
using DocumentManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement.API.Services;

public sealed class DocumentService : IDocumentService
{
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".csv",
        ".json",
        ".md"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/gif",
        "text/csv",
        "application/json",
        "text/markdown",
        "application/octet-stream"
    };

    private readonly DocumentManagementDbContext _dbContext;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(DocumentManagementDbContext dbContext, IFileStorage fileStorage, ILogger<DocumentService> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<DocumentUploadResultDto> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file is null)
        {
            throw new DocumentValidationException("A file is required for upload.");
        }

        if (file.Length == 0)
        {
            throw new DocumentValidationException("The uploaded file is empty.");
        }

        if (file.Length > MaxUploadSizeBytes)
        {
            throw new DocumentValidationException("The uploaded file exceeds the maximum size of 10 MB.");
        }

        var displayName = NormalizeOriginalFileName(file.FileName);
        if (!HasSupportedExtension(displayName))
        {
            throw new DocumentValidationException("The uploaded file type is not supported.");
        }

        var contentType = NormalizeContentType(file.ContentType);
        if (!HasSupportedContentType(contentType))
        {
            throw new DocumentValidationException("The uploaded file content type is not supported.");
        }

        var storedFileName = _fileStorage.BuildSafeStorageName(displayName);

        try
        {
            await _fileStorage.SaveAsync(file.OpenReadStream(), storedFileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store uploaded document '{FileName}'.", displayName);
            throw new DocumentStorageException("The file could not be stored. Please try again.");
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            OriginalFileName = displayName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSize = file.Length,
            UploadedAtUtc = DateTimeOffset.UtcNow
        };

        try
        {
            await _dbContext.Documents.AddAsync(document, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist document metadata for '{FileName}'.", displayName);

            try
            {
                await _fileStorage.DeleteAsync(storedFileName, cancellationToken);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(cleanupException, "Failed to clean up uploaded document '{FileName}' after metadata persistence error.", displayName);
            }

            throw new DocumentStorageException("The document could not be saved to the database.");
        }

        return new DocumentUploadResultDto(document.Id, document.OriginalFileName, document.ContentType, document.FileSize, document.UploadedAtUtc);
    }

    public async Task<IEnumerable<DocumentListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(document => document.UploadedAtUtc)
            .Select(document => new DocumentListItemDto(document.Id, document.OriginalFileName, document.ContentType, document.FileSize, document.UploadedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);

        if (document is null)
        {
            return null;
        }

        return new DocumentDetailDto(document.Id, document.OriginalFileName, document.ContentType, document.FileSize, document.UploadedAtUtc);
    }

    public async Task<DocumentDownloadResult> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);

        if (document is null)
        {
            throw new DocumentNotFoundException(id);
        }

        var exists = await _fileStorage.ExistsAsync(document.StoredFileName, cancellationToken);
        if (!exists)
        {
            throw new DocumentFileMissingException(id);
        }

        var stream = await _fileStorage.OpenReadAsync(document.StoredFileName, cancellationToken);
        return new DocumentDownloadResult(document.OriginalFileName, document.ContentType, stream);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);

        if (document is null)
        {
            throw new DocumentNotFoundException(id);
        }

        try
        {
            var fileExists = await _fileStorage.ExistsAsync(document.StoredFileName, cancellationToken);
            if (fileExists)
            {
                await _fileStorage.DeleteAsync(document.StoredFileName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete physical file for document '{DocumentId}'.", id);
            throw new DocumentStorageException("The document file could not be deleted. Please try again.");
        }

        _dbContext.Documents.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeOriginalFileName(string fileName)
    {
        var normalized = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DocumentValidationException("The uploaded file name is invalid.");
        }

        return normalized;
    }

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "application/octet-stream";
        }

        return contentType.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
    }

    private static bool HasSupportedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension) == false && AllowedExtensions.Contains(extension);
    }

    private static bool HasSupportedContentType(string contentType)
    {
        return AllowedContentTypes.Contains(contentType);
    }
}

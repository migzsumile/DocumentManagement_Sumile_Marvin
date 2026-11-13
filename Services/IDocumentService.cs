using DocumentManagement.API.Models;

namespace DocumentManagement.API.Services;

public interface IDocumentService
{
    Task<DocumentUploadResultDto> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);

    Task<IEnumerable<DocumentListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DocumentDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentDownloadResult> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

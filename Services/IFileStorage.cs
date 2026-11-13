namespace DocumentManagement.API.Services;

public interface IFileStorage
{
    Task SaveAsync(Stream content, string storageName, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageName, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storageName, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string storageName, CancellationToken cancellationToken = default);

    string BuildSafeStorageName(string originalFileName);
}

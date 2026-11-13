namespace DocumentManagement.API.Models;

public sealed record DocumentUploadResultDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTimeOffset UploadedAtUtc);

public sealed record DocumentListItemDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTimeOffset UploadedAtUtc);

public sealed record DocumentDetailDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTimeOffset UploadedAtUtc);

public sealed record DocumentDownloadResult(
    string FileName,
    string ContentType,
    Stream FileStream);

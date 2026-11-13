namespace DocumentManagement.API.Models;

public class Document
{
    public Guid Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTimeOffset UploadedAtUtc { get; set; }
}

namespace DocumentManagement.API.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = @"C:\Apps\LocalFileStorage";
}

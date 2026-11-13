namespace DocumentManagement.API.Services;

public sealed class DocumentValidationException : Exception
{
    public DocumentValidationException(string message)
        : base(message)
    {
    }
}

public sealed class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException(Guid id)
        : base($"Document '{id}' was not found.")
    {
    }
}

public sealed class DocumentFileMissingException : Exception
{
    public DocumentFileMissingException(Guid id)
        : base($"The physical file for document '{id}' is missing.")
    {
    }
}

public sealed class DocumentStorageException : Exception
{
    public DocumentStorageException(string message)
        : base(message)
    {
    }
}

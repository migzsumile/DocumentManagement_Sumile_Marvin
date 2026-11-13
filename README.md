# Document Management API

This project provides a small ASP.NET Core REST API for uploading, listing, retrieving metadata for, downloading, and deleting documents. Document metadata is stored in SQL Server via EF Core, and file bytes are stored in a configurable local storage directory.

## Architecture

- `Controllers/` contains the HTTP endpoints.
- `Context/` contains the EF Core `DbContext`.
- `Models/` contains the document entity and DTOs returned to clients.
- `Services/` contains the document business logic and file-storage abstraction.
- `Options/` binds the configured storage root.

The file system is separated from the database to keep file IO and metadata persistence loosely coupled. When a file is uploaded, the API writes the file to the configured storage root first and then persists metadata. If the database write fails, the implementation attempts to clean up the uploaded file so the system does not leave orphaned files.

## Configuration

The API reads its SQL Server connection string from `ConnectionStrings:DocumentManagementDbContext` and the file-storage root from `FileStorage:RootPath`.

Example `appsettings.json`:

```json
{
  "ConnectionStrings": {
	"DocumentManagementDbContext": "Server=localhost;Database=DocumentManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "FileStorage": {
	"RootPath": "C:\\Apps\\LocalFileStorage"
  }
}
```

To move where documents are stored, update the `RootPath` value. Ensure the target directory exists and the application process has permission to read and write to it. The directory is created automatically when possible.

## Supported file types

This assessment uses a conservative allowlist for upload validation:

- `.txt`, `.md`, `.csv`, `.json`
- `.pdf`
- `.png`, `.jpg`, `.jpeg`, `.gif`

The client-provided filename and content type are treated as untrusted metadata. Extension and MIME validation are used as a practical guardrail, but they are not a guarantee that uploaded content is safe.

## Run the API

1. Ensure SQL Server is available and the connection string is correct.
2. From the `DocumentManagement.API` directory, run:

```bash
dotnet restore
dotnet run
```

3. For this assessment, the application creates the database automatically at startup when the configured SQL Server connection is available via `EnsureCreated()`. The API also redirects HTTP requests to HTTPS in the configured hosting environment.

## Endpoint reference

- `POST /api/documents` — upload a document via `multipart/form-data`
- `GET /api/documents` — list metadata for all documents
- `GET /api/documents/{id}` — retrieve metadata for a document
- `GET /api/documents/{id}/download` — download the stored file
- `DELETE /api/documents/{id}` — delete the document and its file

Response codes:

- `201 Created` — upload succeeded
- `200 OK` — list/detail/download succeeded
- `204 No Content` — delete succeeded
- `400 Bad Request` — invalid upload or missing file
- `404 Not Found` — missing document record or file
- `413 Payload Too Large` — upload exceeds the 10 MB limit
- `500 Internal Server Error` — storage or persistence failure

## Database and storage notes

- Metadata is persisted in SQL Server in the `Documents` table.
- Physical files are stored under the configured `FileStorage:RootPath` using a GUID-based opaque storage name.
- The API never trusts a client-supplied filename as a storage path.
- Download requests resolve the storage path using the trusted database metadata and ensure the final path remains under the configured storage root.
- If a metadata record exists but the physical file is missing, the service returns a controlled `404` response.
- Deletion attempts to remove the physical file first; if the file is already absent, it treats that as a successful cleanup and continues deleting the metadata.

## Rate limiting

This API applies a global fixed-window rate limit of `10 requests per 1 minute per client IP address` using ASP.NET Core rate limiting. Requests above the limit receive HTTP `429 Too Many Requests`.

## Security headers

The API adds the following secure-response headers to applicable responses:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `Strict-Transport-Security: max-age=31536000` only when running in production over HTTPS

The project avoids applying a custom CSP to JSON-only API responses.

## Security review note

The security-review agent identified one high-severity issue related to authentication and authorization. However, this is not a requirement for the technical assessment and is outside the scope of the document-management API implementation requested here.

## Testing

Run the automated tests from the repository root:

```bash
dotnet test DocumentManagement.Test/DocumentManagement.Test.csproj
```

The test project uses MSTest and isolates storage state per test by creating a unique temporary folder. The test host configures a LocalDB SQL Server connection string for the API, so the tests do not depend on a developer's live database.

## AI usage and review

This project is designed to be straightforward and reviewable. AI-assisted review can be used to evaluate correctness, security posture, and test coverage, but implementation decisions should still be validated against the assessment plan and the configured runtime environment.

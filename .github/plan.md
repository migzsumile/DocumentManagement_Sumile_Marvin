# Document Management API — Development Plan

## 1. Goal

Build a clear, maintainable ASP.NET Core REST API that uploads, lists, retrieves, downloads, and deletes documents. Persist document metadata in a relational database and store file contents in a configured file-storage location.

## 2. Requirements checklist

### Functional endpoints

| Method | Endpoint | Expected behavior |
|---|---|---|
| `POST` | `/api/documents` | Accept `multipart/form-data`; validate and store the file; create a unique document ID; persist metadata; return ID and metadata. |
| `GET` | `/api/documents` | Return document metadata only; never include file contents. |
| `GET` | `/api/documents/{id}` | Return metadata for an existing document; return `404` when absent. |
| `GET` | `/api/documents/{id}/download` | Download the stored file; handle a missing database record or missing physical file. |
| `DELETE` | `/api/documents/{id}` | Delete the document and associated file; handle failures without silently leaving inconsistent state. Return `404` when absent. |

### Metadata to persist

- Document ID
- Original filename
- Stored filename or storage identifier
- Content type
- File size
- Upload date and time

### Validation, security, and documentation

- Enforce a maximum upload size of **10 MB per file**.
- Validate required inputs and reject invalid or unsupported files.
- Do not use a client-supplied filename as an unrestricted filesystem path.
- Generate collision-resistant storage names and prevent path traversal.
- Return appropriate HTTP status codes and useful, non-sensitive error responses.
- Document supported file types and validation assumptions.
- Do not commit passwords, tokens, connection-string secrets, or other credentials.
- README must explain setup, database configuration, running, testing, endpoints, architecture, storage, limitations, and AI usage/review.

### Secure HTTP headers

Implement a centralized response-header configuration in ASP.NET Core middleware or the appropriate hosting configuration. Use environment-aware settings:

| Header | Value / configuration | Notes |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevent browsers from MIME-sniffing responses. |
| `X-Frame-Options` | `DENY` | Suitable for this API unless it intentionally serves pages that must be framed. |
| `Referrer-Policy` | `no-referrer` | Avoid sending referrer information. |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | Disable browser features this API does not need. |
| `Strict-Transport-Security` | `max-age=31536000` | Enable only in production when the site is HTTPS-only. Add `includeSubDomains` only if every subdomain is HTTPS-ready. Do not enable HSTS for local HTTP development. |
| `Content-Security-Policy` | Do not apply an arbitrary policy to JSON-only API responses. | If Swagger UI or other browser-rendered pages are enabled, define and test a policy that permits only the resources those pages require. |
| `Server` / `X-Powered-By` | Suppress where supported by the hosting stack. | Defense-in-depth only; do not treat header removal as a primary security control. |

Also:
- Ensure headers are present on applicable success and error responses.
- Verify reverse proxies and hosting configuration do not remove or overwrite headers.
- Configure HTTPS redirection and forwarded headers correctly for the actual hosting topology.
- Do not confuse CORS configuration with security response headers; configure CORS separately and only if browser clients need it.
- Return generic production error responses without stack traces, physical paths, or secrets.

### API rate limiting

Use ASP.NET Core's built-in rate-limiting middleware with a **global fixed-window limit of 10 requests per 1 minute per client IP address**.

Configuration requirements:
- `PermitLimit = 10`
- `Window = TimeSpan.FromMinutes(1)`
- `QueueLimit = 0` so excess requests are rejected rather than queued.
- `AutoReplenishment = true`
- Return HTTP `429 Too Many Requests` when the client exceeds the limit.
- Apply the policy to all document API endpoints unless a documented assessment requirement justifies an exception.
- Partition by the client IP address. Because authentication is not required for this assessment, IP-based partitioning is a practical baseline.
- Trust forwarded client-IP headers only when they come from explicitly configured trusted proxies/networks. Never trust arbitrary forwarded headers from clients.
- Be aware that users behind the same NAT/shared public IP share the same limit.
- The built-in in-memory limiter is per application instance. It is suitable for this single-instance assessment; it does not enforce one shared limit across multiple instances.
- Test that the first 10 requests are permitted, the next request within the same window is rejected with `429`, and a new window permits requests again.
- Keep the 10 MB upload-size limit separate: rate limiting controls request frequency, not request body size or concurrent resource consumption.

Do not describe 10 requests per minute as an ASP.NET Core default; it is the explicit project policy selected for this assessment.

### Minimum automated tests

- Successful upload
- Retrieval of an existing document
- Retrieval of a nonexistent document
- Rejection of an upload larger than 10 MB
- Document deletion

Add focused tests for invalid input, missing physical files, storage errors, and persistence errors if time permits.

## 3. Proposed implementation

Use a deliberately small, solution-root structure with the API project and its test project as sibling directories. Adapt names to the chosen solution and installed SDK.

```text
DocumentManagement/
├── DocumentManagement.Api/
│   ├── Controllers/                  # DocumentsController
│   ├── Data/                         # DbContext and EF Core configuration
│   ├── Models/                       # Document entity and response DTOs
│   ├── Services/                     # Document service and file-storage abstraction
│   ├── Storage/                      # Local file-system implementation
│   ├── Options/                      # Upload/storage options
│   ├── Program.cs
│   └── appsettings.json              # Non-secret defaults only
|   ├── .gitignore
|   ├── README.md
|   └── DocumentManagement.sln		
├── DocumentManagement.Api.Tests/     # Create unit test in separate project

```

### Design choices

1. **ASP.NET Core Web API:** use controllers or minimal APIs consistently. Controllers are a straightforward fit for the five resource endpoints.
2. **EF Core + SQL Server:** store metadata in a `Documents` table and use an EF Core migration for schema creation.
3. **Storage abstraction:** define a small `IFileStorage` interface with save, open/read, exists, and delete operations. Implement it using a configured local directory. This keeps file I/O out of the controller and makes tests easier.
4. **Configurable local file storage:** use `C://Apps/LocalFileStorage` as the initial storage root. Define the path in `appsettings.json` (for example, `FileStorage:RootPath`) and read it through a strongly typed options class or configuration binding. If the storage location needs to change, update the value in `appsettings.json` for the relevant environment; do not hardcode the path in application code. Ensure the configured directory exists or create it safely at startup/use, and grant the application process appropriate filesystem permissions.
5. **Opaque storage name:** generate a GUID-based stored name; preserve the original filename only as metadata. Resolve paths under the configured storage root and never concatenate an untrusted filename into a path.
5. **DTOs:** return metadata DTOs rather than entity internals or file bytes from list/detail endpoints.
6. **Dependency injection and options:** register the DbContext, document service, storage service, and validated upload/storage options in `Program.cs`.
7. **Testing:** use xUnit and `WebApplicationFactory` for API-level tests where practical. Use a temporary test directory and an isolated test database/provider. Do not let tests write to the developer's real storage folder.

## 4. Important implementation decisions

### File storage configuration

Set the initial local storage directory in `appsettings.json`:

```json
{
  "FileStorage": {
    "RootPath": "C://Apps/LocalFileStorage"
  }
}
```

- Bind `FileStorage:RootPath` to a typed options class and inject it into the storage implementation.
- To change the storage location, update `FileStorage:RootPath` in the applicable `appsettings.json` (or environment-specific appsettings file). No source-code change should be required.
- Keep the path configurable; do not duplicate or hardcode it in controllers or services.
- Ensure the application identity has read/write permissions to the configured directory.
- Keep uploaded files outside the web root and do not expose the directory as static content.
- Document the configured path and how to change it in `README.md`. Do not commit real user documents or sensitive files.

### Upload validation

- Enforce the 10 MB limit in application validation and configure the request/form limits so the server does not accept a larger multipart body unintentionally. Account for multipart overhead when configuring the overall request limit.
- Reject an empty file and missing upload input.
- Define a small, explicit allowlist of file extensions and content types in the README. The assessment does not prescribe supported file types, so state the assumption clearly.
- Treat the client-provided content type and extension as untrusted metadata. For a time-limited assessment, document the limits of validation; do not claim that checking an extension or MIME header proves a file is safe.
- Normalize and store the original filename as display metadata only. Do not use it as a storage path.

### Upload consistency

1. Validate the request before writing anything.
2. Generate the document ID and opaque storage name.
3. Write the file using a unique name and exclusive creation semantics where available.
4. Save metadata to the database.
5. If metadata persistence fails, attempt to remove the newly written file and log cleanup failure. Do not expose internal paths or exception details to the client.
6. Return a `201 Created` response with a location pointing to the document detail endpoint.

A database and file system do not share a transaction. Explain this compensating-cleanup approach as a pragmatic consistency measure, not as a distributed transaction.

### Download behavior

- Return `404` if the metadata record does not exist.
- If the metadata exists but the physical file is missing, return a controlled error (for example, `404`) and log the inconsistency. Document the chosen behavior.
- Use the stored opaque name from trusted database metadata and validate that the resolved path remains inside the configured storage root.
- Set the download filename from the stored original filename as a download header, not as a filesystem path.

### Delete consistency

Choose and document a clear deletion sequence. A pragmatic approach is to locate the record, attempt file deletion, and then remove metadata. If file deletion fails, retain the metadata and return a controlled server error so the operation can be retried. If the file is already absent, treat that as an idempotent cleanup condition and remove the metadata. Be explicit about exceptional cases and test the behavior.

### HTTP responses

- `201 Created` — upload succeeds.
- `200 OK` — list/detail succeeds; download returns the file.
- `204 No Content` — delete succeeds.
- `400 Bad Request` — invalid or missing upload input.
- `404 Not Found` — document record or download file is absent, according to the documented policy.
- `413 Payload Too Large` — request exceeds the configured upload limit.
- `500 Internal Server Error` — unexpected storage or persistence failure; return a generic problem response, not stack traces or physical paths.

Use consistent Problem Details responses for errors if supported by the chosen ASP.NET Core version.
.
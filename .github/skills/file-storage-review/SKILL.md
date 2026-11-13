---
name: file-storage-review
description: Review local file storage implementation for safe paths, upload limits, collision resistance, async I/O, and consistency with SQL metadata.
---

# File Storage Review

## Purpose
Review file upload, storage, download, and deletion behavior for the Document Management API. Apply the explicit storage and validation constraints in `plan.md`.

## Required context
- Read `plan.md` and `copilot-instructions.md`.
- Inspect `IFileStorage`, its local filesystem implementation, options, upload validation, download flow, delete flow, and relevant tests.
- Confirm the configured root path and actual hosting environment before making environment-specific claims.

## Critical requirements
- Maximum upload size: 10 MB per file.
- Never trust a user-supplied filename as a filesystem path.
- Prevent path traversal.
- Prevent filename collisions.
- Store files outside the web root; do not expose the storage directory as static content.
- Keep the storage root configurable through `FileStorage:RootPath`, initially `C://Apps/LocalFileStorage`; do not hardcode it in controllers or services.
- Use asynchronous file operations where supported.
- Maintain database/file consistency using the documented compensating behavior.

## Review checklist

### Upload and validation
- Reject missing and empty files.
- Enforce the 10 MB limit in application validation and configure request/form limits so larger multipart bodies are not unintentionally accepted. Account for multipart overhead.
- Check a small, explicit extension/content-type allowlist and verify it is documented.
- Treat client filename, extension, and content type as untrusted. Extension/MIME validation is not proof that file content is safe.
- Normalize the original filename for display metadata only; never use it to create or resolve a storage path.
- Generate a GUID-based or otherwise collision-resistant opaque stored name.
- Use exclusive creation semantics where available so an existing file is not overwritten.
- Validate options and ensure the root directory exists safely with appropriate process permissions.

### Path safety
- Resolve storage paths only from trusted opaque storage identifiers and the configured root.
- Canonicalize the resolved path and verify it remains inside the configured storage root, including separator/boundary handling.
- Reject malformed storage identifiers rather than allowing path fragments.
- Check symlink/reparse-point and filesystem assumptions where relevant to the deployment; distinguish proven vulnerabilities from environment-dependent risks.
- Never expose physical paths to API clients.

### Download
- Return `404` for a missing metadata record.
- If metadata exists but the physical file is missing, follow the documented controlled-error policy (the plan gives `404` as an example) and log the inconsistency.
- Use the stored opaque name from trusted database metadata to locate the file.
- Set the download filename from the original filename as a response download header, not as a filesystem path.
- Avoid loading large files fully into memory when streaming is practical.

### Delete and consistency
- Attempt physical file deletion before metadata removal.
- If file deletion fails, retain metadata and return a controlled server error so deletion can be retried.
- If the file is already absent, treat it as idempotent cleanup and remove metadata.
- On upload, write the uniquely named file before saving metadata; if persistence fails, attempt to delete the new file and log cleanup failure.
- Do not claim the database and filesystem share an atomic transaction.

### Abstraction and tests
- Confirm the planned `IFileStorage` abstraction supports save, open/read, exists, and delete.
- Keep filesystem code out of controllers.
- Tests should use a temporary isolated directory, never the developer's configured storage directory.
- Test oversized/empty/invalid uploads, path traversal attempts, collisions, missing files, storage errors, and cleanup behavior where practical.

## Output format
- **Verdict:** Pass / Pass with concerns / Needs changes.
- **Critical findings:** Path traversal, overwrite/collision, data loss, or unbounded upload risks.
- **Other findings:** Reliability, async I/O, configuration, or testability.
- **Failure-path matrix:** Upload, download, delete, and persistence/storage failure behavior.
- **Evidence and limits:** Cite code locations and state what was not exercised at runtime.

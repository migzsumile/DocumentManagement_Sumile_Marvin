---
name: api-design-review
description: Review the Document Management API's REST endpoint design, HTTP semantics, DTOs, validation, and error responses against the project plan.
---

# API Design Review

## Purpose
Review the Document Management REST API for clear, consistent, assessment-appropriate HTTP and REST design. Base the review on the project's `copilot-instructions.md` and `plan.md`; do not introduce requirements that those files do not establish.

## Required context
1. Read `plan.md` before reviewing implementation.
2. Inspect the document controller or endpoint definitions, request/response DTOs, validation, exception handling, and relevant service contracts.
3. Confirm the implementation sequence and existing framework conventions before suggesting changes.

## Review checklist

### Required routes and behavior
- `POST /api/documents`: accepts `multipart/form-data`, validates and stores the file, creates a unique document ID, persists metadata, and returns the ID and metadata.
- `GET /api/documents`: returns metadata only; never returns file contents.
- `GET /api/documents/{id}`: returns metadata for an existing document and `404` when absent.
- `GET /api/documents/{id}/download`: streams/downloads the stored file; handles a missing record and a missing physical file according to the documented policy.
- `DELETE /api/documents/{id}`: removes the document and associated file; returns `404` when the record is absent and does not silently leave inconsistent state.

### HTTP and contract quality
- Check correct status codes: `201 Created`, `200 OK`, `204 No Content`, `400 Bad Request`, `404 Not Found`, `413 Payload Too Large`, and generic `500` responses for unexpected failures, as applicable.
- Successful upload should return a `Location` pointing to the document detail endpoint.
- Use request/response DTOs rather than exposing EF entities or internal storage details.
- Ensure list/detail responses expose metadata only, including the planned fields where appropriate: document ID, original filename, content type, file size, and upload date/time.
- Keep route naming, response shapes, nullability, and validation behavior consistent across endpoints.
- Use Problem Details consistently for errors if supported by the selected ASP.NET Core version.
- Error responses must be useful but non-sensitive; never include stack traces, physical paths, credentials, or implementation details.

### Input and validation
- Reject missing and empty upload input.
- Enforce the 10 MB per-file limit in application validation and configure multipart/request limits to prevent larger bodies being accepted unintentionally. Account for multipart overhead.
- Validate against a small, explicit allowlist of extensions and content types. The plan leaves the supported types as an implementation assumption; verify that the assumption is documented.
- Treat filename, extension, and client-supplied content type as untrusted. Do not imply extension/MIME checks prove a file is safe.

### Cross-cutting behavior
- Confirm global fixed-window rate limiting is applied to document endpoints: 10 requests per minute per client IP, queue limit 0, automatic replenishment, and `429` on excess requests.
- Confirm response headers and HTTPS behavior are configured centrally and are present on applicable success and error responses.
- Do not conflate CORS with security response headers.
- Authentication and authorization are explicitly not required for this assessment; do not report their absence as a defect.

## Review method
1. Trace each route from HTTP binding through validation, service call, and response mapping.
2. Compare actual behavior with the route table and response policy in `plan.md`.
3. Check edge cases: malformed IDs, absent records, missing physical files, invalid multipart requests, oversized payloads, and storage/persistence exceptions.
4. Identify only actionable defects or meaningful assessment risks. Avoid stylistic rewrites and unnecessary architecture.

## Output format
- **Verdict:** Pass / Pass with concerns / Needs changes.
- **Findings:** For each, include severity, endpoint/file and relevant code location, observed behavior, why it conflicts with the project plan, and a concrete minimal fix.
- **Coverage:** Table or bullets mapping each required endpoint to its verified behavior and any gap.
- **Unverified assumptions:** Explicitly list behavior that could not be established from the inspected code or tests.
- **Do not claim runtime verification** unless tests or execution evidence were actually observed.

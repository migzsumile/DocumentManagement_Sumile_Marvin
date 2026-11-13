---
name: final-assessment-review
description: Perform a final submission-readiness assessment of the Document Management API against all requirements in the project plan.
---

# Final Assessment Review

## Purpose
Determine whether the Document Management API is ready for technical assessment/submission. Treat `plan.md` as the acceptance baseline and `copilot-instructions.md` as project-wide development guidance.

## Required context
- Read both project instruction files before reviewing.
- Inspect source, configuration, migrations, tests, README, and repository status where available.
- Do not assume a checklist item is complete because it is documented; seek implementation or execution evidence.

## Acceptance checklist

### Functional API
- [ ] `POST /api/documents` accepts multipart form data, validates/stores file, persists metadata, and returns ID/metadata with `201 Created` and a detail `Location`.
- [ ] `GET /api/documents` returns metadata only.
- [ ] `GET /api/documents/{id}` returns metadata or `404`.
- [ ] `GET /api/documents/{id}/download` downloads the file and handles missing record/file safely.
- [ ] `DELETE /api/documents/{id}` deletes file and metadata with the documented failure policy; absent record returns `404`.

### Data, storage, and validation
- [ ] SQL Server + EF Core persist all planned metadata.
- [ ] File storage is abstracted behind `IFileStorage` or equivalent and uses configurable `FileStorage:RootPath`.
- [ ] Initial configured path is `C://Apps/LocalFileStorage` unless a documented environment-specific override is in effect.
- [ ] Files are outside web root.
- [ ] 10 MB per-file limit is enforced at application and request/form layers, accounting for multipart overhead.
- [ ] Empty/missing inputs and unsupported files are rejected.
- [ ] Explicit extension/content-type allowlist and assumptions are documented.
- [ ] Client filenames are metadata only; storage names are opaque, collision-resistant, and path-safe.
- [ ] Upload compensation and delete failure behavior preserve a retryable, non-silent consistency state.

### Security and operational controls
- [ ] No credentials or secrets are committed.
- [ ] Response headers are centrally configured: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, and `Permissions-Policy: camera=(), microphone=(), geolocation=()`.
- [ ] HSTS is enabled only in production when HTTPS-only; do not enable it for local HTTP development. Add `includeSubDomains` only when every subdomain is HTTPS-ready.
- [ ] No arbitrary CSP is applied to JSON-only responses. If Swagger/UI pages exist, CSP is tailored and tested for those pages.
- [ ] Suppress `Server`/`X-Powered-By` where supported as defense-in-depth only.
- [ ] Headers are present on applicable success/error responses; reverse proxy behavior is checked.
- [ ] HTTPS redirection and forwarded headers match the hosting topology.
- [ ] CORS is treated separately and configured only when browser clients require it.
- [ ] Generic production errors do not reveal stack traces, physical paths, or secrets.
- [ ] Global fixed-window IP rate limiting is configured: 10 requests / 1 minute, queue 0, auto replenishment, `429`; trusted forwarded IP headers only from configured proxies/networks.
- [ ] Rate limiting is applied to all document endpoints unless an assessment exception is documented; single-instance in-memory limitation and shared-NAT behavior are understood.

### Engineering and tests
- [ ] Dependency injection is used appropriately.
- [ ] Database and file operations use async APIs where supported.
- [ ] DTOs are used for API requests/responses.
- [ ] HTTP status codes and error handling are appropriate.
- [ ] Architecture remains small and assessment-appropriate; no unnecessary complexity.
- [ ] Automated tests cover successful upload, existing-document retrieval, nonexistent-document retrieval, oversized upload, and deletion.
- [ ] Focused tests for invalid input, missing physical files, storage failures, and persistence failures are added where time permits.
- [ ] Tests use an isolated database/provider and temporary storage; they do not write to the developer's real storage folder.

### Submission documentation and verification
- [ ] README explains setup, prerequisites, database configuration, running, testing, endpoints, architecture, storage, limitations, and AI usage/review.
- [ ] Build succeeds.
- [ ] All automated tests pass.
- [ ] All required endpoints are verified.
- [ ] No secrets are committed.
- [ ] README matches actual behavior.

## Assessment rules
- Mark each item **Pass**, **Fail**, **Partial**, or **Not verified**.
- A source-code inspection is not equivalent to a successful build, passing test, or endpoint execution.
- Distinguish plan requirements from optional hardening.
- Authentication and authorization are not required; do not fail the assessment for their absence.
- Do not silently waive explicit plan requirements. If an item is incomplete, state the impact and smallest practical remediation.
- Do not invent evidence, line numbers, test results, or deployment facts.

## Output format
1. **Overall readiness:** Ready / Ready with caveats / Not ready.
2. **Scorecard:** Requirement, status, evidence, gap/action.
3. **Blockers:** Issues that should be fixed before submission, ordered by severity.
4. **Non-blocking improvements:** Clearly separated from required work.
5. **Verification record:** Exact build/test/endpoint commands run and their observed results; otherwise mark Not run.
6. **Submission recommendation:** A concise evidence-based conclusion.

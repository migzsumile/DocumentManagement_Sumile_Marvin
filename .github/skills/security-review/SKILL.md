---
name: security-review
description: Review upload, filesystem, HTTP, configuration, error handling, and rate-limiting security for the Document Management API.
---

# Security Review

## Purpose
Review security risks in the Document Management API using the explicit constraints in `copilot-instructions.md` and `plan.md`. Focus on realistic risks in file upload/download/delete and HTTP deployment.

## Scope boundaries
- Authentication and authorization are explicitly not required for this technical assessment. Do not flag their absence as a plan violation.
- Do not claim file-extension or MIME-type checks establish that content is safe.
- Distinguish required controls from optional defense-in-depth suggestions.

## Critical review checklist

### Upload validation and file handling
- Enforce maximum 10 MB per file in application validation and request/form configuration; account for multipart overhead.
- Reject missing and empty files and unsupported inputs.
- Use an explicit, documented allowlist of extensions and content types.
- Treat client filename, extension, and content type as untrusted.
- Preserve normalized original filename only as display metadata.
- Generate an opaque, collision-resistant storage name and use exclusive creation semantics where available.
- Prevent path traversal; never concatenate an untrusted filename into a filesystem path.
- Keep uploaded files outside the web root and do not expose the storage directory as static content.
- Consider safe download headers and ensure the download filename is metadata/header content, never a path.

### Storage and consistency
- Bind `FileStorage:RootPath` to typed configuration; initial plan value is `C://Apps/LocalFileStorage`.
- Ensure resolved paths remain within the canonical configured root.
- Check malformed storage identifiers and relevant symlink/reparse-point assumptions.
- Ensure application filesystem permissions are limited to what is required.
- Review upload compensating cleanup if metadata persistence fails; log cleanup failure without disclosing paths/details to clients.
- Review delete sequence: retain metadata if file deletion fails; treat already-missing file as idempotent cleanup.
- Do not claim atomic database/filesystem transactions.

### HTTP and deployment
- Verify centralized response headers on applicable success and error responses:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy: no-referrer`
  - `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- Enable HSTS only in production when HTTPS-only; do not enable for local HTTP development. Add `includeSubDomains` only if every subdomain is HTTPS-ready.
- Do not apply an arbitrary CSP to JSON-only API responses. If Swagger/UI pages exist, define and test a suitable policy.
- Suppress `Server` and `X-Powered-By` where supported, treating this as defense-in-depth rather than a primary control.
- Check HTTPS redirection and forwarded headers for the actual hosting topology.
- Trust forwarded client-IP headers only from explicitly configured trusted proxies/networks.
- Keep CORS separate from security headers and configure it only if browser clients need it.
- Ensure production errors are generic and never reveal stack traces, physical paths, secrets, or connection strings.

### Rate limiting
- Verify ASP.NET Core built-in global fixed-window rate limiting:
  - `PermitLimit = 10`
  - `Window = TimeSpan.FromMinutes(1)`
  - `QueueLimit = 0`
  - `AutoReplenishment = true`
  - partition key is client IP
  - excess requests receive `429 Too Many Requests`
- Apply to all document API endpoints unless a documented assessment reason justifies an exception.
- Do not call this an ASP.NET Core default; it is the project's explicit policy.
- Explain shared-NAT impact and the in-memory per-instance limitation.

### Secrets and configuration
- Search tracked configuration and repository content for committed passwords, tokens, connection-string secrets, certificates, and other credentials.
- Ensure examples contain placeholders and real secrets are supplied outside source control.
- Check logs and error responses for accidental secret/path disclosure.

## Review method
1. Map each finding to an asset, threat, code path, and plan requirement.
2. Trace untrusted input to filesystem, response headers, and persistence.
3. Review middleware ordering and proxy assumptions where visible.
4. Prefer concrete exploitability and impact over generic security advice.
5. Never run destructive tests against real storage or production data.

## Output format
- **Verdict:** Pass / Pass with concerns / Needs changes.
- **Critical/high findings:** Exploit path, impact, evidence, and minimal remediation.
- **Other findings:** Defense-in-depth or deployment concerns.
- **Control matrix:** Requirement, implementation evidence, status.
- **Not assessed:** Any runtime, hosting, proxy, or infrastructure assumptions that could not be verified.

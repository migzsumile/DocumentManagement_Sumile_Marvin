---
name: documentation-review
description: Review README and API documentation for accuracy and completeness against the Document Management API plan and implementation.
---

# Documentation Review

## Purpose
Review project documentation, especially `README.md`, for completeness, accuracy, reproducibility, and consistency with the implemented Document Management API.

## Required context
- Read `plan.md` and `copilot-instructions.md` first.
- Inspect the actual code, configuration, routes, validation rules, and tests before asserting that documentation matches implementation.
- Treat the plan as the source of intended requirements; distinguish intended behavior from behavior actually verified in code.

## Required README coverage
Verify that the README explains:
1. Project purpose and architecture.
2. Prerequisites and setup instructions, including the relevant .NET SDK/runtime and SQL Server assumptions actually used by the project.
3. Database configuration and how to provide connection strings without committing secrets.
4. EF Core migration/database creation steps appropriate to the implementation.
5. How to build, run, and test the solution.
6. All five endpoints, HTTP methods, routes, request content type, expected success responses, and meaningful error responses.
7. Metadata fields returned and confirmation that list/detail endpoints do not return file contents.
8. File storage configuration, including the initial `FileStorage:RootPath` value `C://Apps/LocalFileStorage`, how to change it per environment, required filesystem permissions, and the fact that files are outside the web root.
9. The 10 MB per-file upload limit and multipart overhead/request limit behavior.
10. The implemented extension/content-type allowlist and its validation assumptions. The plan does not prescribe file types, so ensure the chosen allowlist is explicitly documented and do not describe MIME/extension checks as malware protection.
11. Error handling and the chosen policy for missing physical files and deletion failures.
12. Rate limiting: global fixed window, 10 requests per minute per client IP, queue limit 0, `429`, trusted-proxy considerations, shared-NAT impact, and the single-instance/in-memory limitation.
13. Secure response headers and environment-aware HSTS behavior, plus HTTPS/forwarded-header deployment assumptions where relevant.
14. Known limitations and AI usage/review, as explicitly requested by the plan.

## Accuracy checks
- Confirm every documented route, configuration key, default, command, package, and behavior exists in the implementation.
- Do not document authentication/authorization as implemented or required; the assessment explicitly excludes them.
- Do not describe 10 requests/minute as an ASP.NET Core default; it is the project's explicit policy.
- Do not claim a distributed transaction between SQL Server and the filesystem. Describe upload compensation/cleanup as a pragmatic consistency measure.
- Avoid examples containing real secrets, tokens, passwords, personal documents, or sensitive paths beyond the project's stated configurable default.
- Keep examples copyable and mark placeholders clearly.

## Review method
1. Build a documentation coverage checklist from the required README topics.
2. Cross-check each factual statement against code/configuration/tests.
3. Identify missing, stale, contradictory, or unverified instructions.
4. Prefer precise edits over wholesale rewrites.

## Output format
- **Verdict:** Pass / Pass with concerns / Needs changes.
- **Coverage matrix:** Topic, documented?, matches implementation?, evidence/gap.
- **Findings:** Severity, section, issue, impact, proposed correction.
- **Suggested README edits:** Provide ready-to-paste wording only where useful.
- **Unverified claims:** List commands or deployment steps not actually tested.

---
name: unit-testing
description: Design and review focused automated tests for the Document Management API using xUnit, WebApplicationFactory, isolated storage, and an isolated database.
---

# Unit Testing and Test Review

## Purpose
Create or review reliable automated tests for the Document Management API. Use the plan's minimum coverage and favor isolated, deterministic tests.

## Required context
- Read `plan.md` before adding or changing tests.
- Inspect the actual target framework, package versions, dependency injection setup, endpoint style, and existing test conventions.
- The plan proposes MSTest and `WebApplicationFactory` where practical; adapt to the installed SDK and solution rather than assuming versions.

## Test architecture
- Use MSTest if consistent with the project plan and installed dependencies.
- Use `WebApplicationFactory` for API-level/integration tests where practical.
- Use a temporary test directory for file storage.
- Use an isolated test database/provider. Do not let tests connect to or mutate a developer's real database or storage directory.
- Replace configuration and dependencies through the test host/DI rather than relying on machine-specific settings.
- Keep tests independent, repeatable, and safe to run in parallel only when their resources are isolated.

## Minimum required coverage
1. Successful upload.
2. Retrieval of an existing document.
3. Retrieval of a nonexistent document (`404`).
4. Rejection of an upload larger than 10 MB (`413` or the documented consistent response).
5. Successful document deletion.

## Focused additional coverage
- Missing upload input and empty file.
- Unsupported extension/content type based on the documented allowlist.
- Exactly-at-limit and over-limit upload behavior.
- List endpoint returns metadata only and does not expose file bytes.
- Download succeeds and uses the original filename as download metadata/header.
- Download when the database record is absent.
- Download when metadata exists but the physical file is missing.
- Path traversal attempts and malformed storage identifiers.
- Storage-name collision/overwrite prevention.
- Storage write/read/delete failures.
- Database persistence failure after a successful file write, including compensating cleanup and cleanup failure logging.
- Delete failure retains metadata; already-missing file follows the documented idempotent cleanup policy.
- Rate limiter permits the first 10 requests in a window, rejects the next with `429`, and permits requests after replenishment.
- Secure headers appear on applicable success and error responses.
- Generic production errors do not expose stack traces, physical paths, or secrets.

## Test quality
- Assert externally observable behavior, not private implementation details.
- Use descriptive names that state scenario and expected result.
- Arrange, act, assert consistently; keep fixtures and builders small.
- Avoid sleeps and timing-sensitive tests. For rate limiting, use a controllable/testable time strategy if supported by the target framework; otherwise keep the test robust and document any limitations.
- Clean up temporary directories and test database resources even when tests fail.
- Do not assert unspecified file types or undocumented assumptions.
- Ensure upload size tests account for multipart overhead and distinguish the per-file limit from total request limit.
- Keep test data synthetic; never use real user documents or secrets.

## Review method
1. Map tests to the minimum and additional coverage lists.
2. Verify each test actually reaches the intended code path and asserts the required outcome.
3. Run the relevant test command only when execution is available; record the exact command and result.
4. Identify gaps and propose the smallest high-value tests first.

## Output format
- **Coverage summary:** Required scenario, test present, assertion quality, result.
- **Findings:** Missing, flaky, unsafe, or misleading tests with severity and location.
- **Recommended tests:** Provide concrete test names and concise Arrange/Act/Assert outlines.
- **Execution evidence:** Exact test command, pass/fail/skip counts, or clearly state Not run.

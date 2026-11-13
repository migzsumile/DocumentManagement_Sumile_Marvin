---
name: code-review
description: Review implementation quality, maintainability, SOLID usage, dependency injection, async behavior, error handling, and assessment scope.
---

# Code Review

## Purpose
Perform a focused code-quality review of the Document Management API. Keep recommendations proportionate to the technical assessment and the deliberately small architecture described in `plan.md`.

## Required context
- Read `plan.md` before reviewing features.
- Read `copilot-instructions.md` and follow its development guidelines.
- Inspect relevant callers, tests, configuration, and abstractions before judging an isolated method.

## Review checklist

### Correctness and maintainability
- Verify code does what the endpoint and service contract promise, including failure paths.
- Check clear naming, cohesive methods, appropriate separation of responsibilities, and consistent conventions.
- Apply SOLID principles where they improve clarity; do not force patterns or add layers without a concrete need.
- Keep controllers focused on HTTP concerns; keep file I/O out of controllers through the planned storage abstraction.
- Avoid duplicated validation, path construction, response mapping, or error handling where a small shared implementation is clearer.

### Dependency injection and configuration
- Confirm services, `DbContext`, storage implementation, and options are registered through dependency injection.
- Prefer strongly typed options for upload and storage configuration where appropriate.
- Ensure configuration is not duplicated or hardcoded in controllers/services.
- Keep `appsettings.json` defaults non-secret. Do not commit credentials or secrets.

### Asynchronous operations
- Use asynchronous APIs for database and file operations where supported.
- Propagate cancellation tokens through request-driven async work where practical.
- Avoid `.Result`, `.Wait()`, unnecessary `Task.Run` around naturally asynchronous I/O, and `async void`.
- Do not claim an operation is asynchronous merely because its wrapper returns `Task`.

### Error handling and observability
- Check that exceptions are handled at an appropriate boundary and translated into generic, non-sensitive HTTP errors.
- Preserve useful server-side diagnostics without returning exception details, stack traces, or physical paths to clients.
- Check cleanup failures are logged and do not mask the original failure.
- Avoid broad catch blocks that silently swallow errors or convert all failures into misleading client errors.

### Scope and design
- Prefer the plan's small structure: controllers, data, models/DTOs, services, storage, options, and tests.
- The planned `IFileStorage` abstraction covers save, open/read, exists, and delete operations.
- Avoid adding CQRS, repositories, mediator frameworks, distributed infrastructure, or elaborate domain layers unless the existing code or explicit assessment requirement justifies them.
- Authentication and authorization are not required by this assessment; do not flag their absence.

## Review method
1. Follow important execution paths end-to-end.
2. Check both happy paths and failure/cleanup paths.
3. Verify configuration and DI registrations against actual consumers.
4. Separate correctness/security defects from optional style preferences.
5. Recommend the smallest safe change that resolves each material issue.

## Output format
- **Verdict:** Pass / Pass with concerns / Needs changes.
- **Blocking findings:** Correctness, data-loss, security, or reliability problems.
- **Non-blocking findings:** Maintainability or clarity improvements that materially help.
- **Positive observations:** Sound decisions worth preserving.
- **Out of scope / not assessed:** Explicitly state what was not inspected or executed.
Each finding should include severity, location, evidence, impact, and a concrete suggested fix. Do not invent line numbers or claim tests/builds ran without evidence.

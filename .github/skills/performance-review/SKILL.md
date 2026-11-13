---
name: performance-review
description: Review API, EF Core, upload/download, storage, and rate-limiting performance without adding unnecessary architecture.
---

# Performance Review

## Purpose
Identify measurable or credible performance risks in the Document Management API while preserving the project's small, assessment-appropriate design.

## Required context
- Read `plan.md` and `copilot-instructions.md`.
- Inspect endpoint code, EF Core queries, file I/O, request limits, rate limiting, and tests.
- Prefer evidence and workload context over speculative micro-optimization.

## Review checklist

### API and database
- List/detail endpoints should retrieve metadata only, not file contents.
- Check for unnecessary entity tracking on read-only paths where it has a meaningful cost.
- Check for N+1 queries, repeated database round trips, or loading more metadata than the response needs.
- Ensure database I/O is asynchronous.
- Do not prescribe pagination as a plan requirement; identify it only as a scale-dependent consideration if the current unbounded list is a demonstrated concern.

### File operations and memory
- Enforce the 10 MB per-file limit independently of rate limiting.
- Avoid buffering entire files in memory when streaming is practical, especially on download.
- Check upload buffering and multipart request limits, including multipart overhead.
- Use async stream operations and dispose streams/resources reliably.
- Avoid unnecessary copies, repeated hashing, or repeated filesystem metadata checks without a reason.
- Ensure concurrency does not cause collisions or accidental overwrites; use unique names and exclusive creation semantics where available.

### Rate limiting and resource protection
- Verify the explicit global fixed-window policy: 10 requests per minute per client IP, one-minute window, queue limit 0, auto replenishment, and `429`.
- Confirm the policy applies to all document endpoints unless an exception is documented.
- Treat request frequency and request-body/concurrent upload resource limits as separate concerns.
- Check trusted-proxy configuration before relying on forwarded client IP values.
- Note that the built-in in-memory limiter is per application instance and does not provide a shared multi-instance limit.

### Configuration and complexity
- Verify storage path and other operational settings are configurable.
- Avoid adding distributed caches, queues, background workers, or infrastructure not justified by the assessment or evidence.
- Identify likely bottlenecks and explain their impact under a plausible workload.

## Review method
1. Trace the largest data paths: upload, list, download, and delete.
2. Identify memory, I/O, query, and contention costs.
3. Use benchmarks, logs, query plans, or tests when available; otherwise label findings as code-based risks.
4. Prioritize fixes by expected impact and implementation cost.

## Output format
- **Verdict:** Pass / Pass with concerns / Needs changes.
- **Findings:** Severity, location, evidence, expected impact, and minimal fix.
- **Hot-path summary:** Upload, list, download, delete.
- **Evidence quality:** Measured result vs reasoned risk vs unverified assumption.
- **Avoided overengineering:** Note where the current simple design is appropriate.

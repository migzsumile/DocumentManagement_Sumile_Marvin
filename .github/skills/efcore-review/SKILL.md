---
name: efcore-review
description: Review EF Core and SQL Server persistence, entity mapping, migrations, async queries, and database/file consistency.
---

# EF Core Review

## Purpose
Review the Document Management API's Entity Framework Core implementation and SQL Server persistence against `plan.md`.

## Required context
- Read `plan.md` before reviewing implementation.
- Inspect the `DbContext`, entity, configurations, migrations, service queries, database provider setup, and persistence tests.
- Do not assume a particular EF Core version or schema detail unless visible in the project.

## Review checklist

### Model and schema
- Confirm document metadata is persisted in a `Documents` table (or clearly equivalent mapping).
- Verify persistence of the planned fields: document ID, original filename, opaque stored filename/storage identifier, content type, file size, and upload date/time.
- Check key type/generation, required fields, maximum lengths, nullability, and sensible database column types.
- Ensure the stored filename/storage identifier is distinct from the client-supplied original filename.
- Check indexes and constraints only where they serve actual query or integrity needs; avoid speculative schema complexity.

### DbContext and configuration
- Confirm SQL Server provider configuration uses externalized connection-string configuration.
- Ensure credentials are not committed and examples use placeholders or secret-management mechanisms appropriate to the environment.
- Verify `DbContext` registration and lifetime are appropriate for ASP.NET Core request processing.
- Confirm migrations represent the current model and are included/usable for schema creation.

### Query behavior
- List and detail queries should project to metadata DTOs and never load/return file bytes unnecessarily.
- Verify missing detail records produce the planned `404` behavior at the API boundary.
- Check filtering, ordering, pagination only if present or required; do not invent pagination as a project requirement.
- Use async EF Core operations for database I/O and pass cancellation tokens where practical.
- Avoid unnecessary tracking for read-only queries when it improves the implementation without obscuring correctness.

### Persistence and consistency
- Review upload ordering: write file, save metadata, and on persistence failure attempt compensating deletion of the newly written file; log cleanup failure.
- Recognize SQL Server and the filesystem do not share a transaction. Do not describe compensation as a distributed transaction.
- Review delete ordering: attempt file deletion before removing metadata; if deletion fails, retain metadata and return a controlled server error. If the file is already absent, remove metadata as idempotent cleanup.
- Check persistence exceptions are not leaked to clients and do not leave silent inconsistencies.
- Consider concurrency and duplicate-ID behavior only to the extent supported by the implementation.

## Review method
1. Trace each create/read/delete data path.
2. Compare entity and migration schema to the planned metadata.
3. Inspect generated SQL or tests only if available; do not infer runtime behavior from source alone.
4. Verify database error handling and compensating cleanup boundaries.

## Output format
- **Verdict:** Pass / Pass with concerns / Needs changes.
- **Findings:** Severity, file/location, evidence, impact, minimal fix.
- **Schema coverage:** Planned metadata field to entity/column mapping.
- **Consistency assessment:** Upload and delete failure-path behavior.
- **Unverified items:** Migration execution, provider behavior, or database integration not actually tested.

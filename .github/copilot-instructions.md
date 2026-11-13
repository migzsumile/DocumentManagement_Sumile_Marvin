
# Project Instructions

## Project Overview
This project is a Document Management REST API
developed using ASP.NET Core, C#, EF Core, and SQL Server.

## Development Guidelines

1. Read `plan.md` before implementing features.
2. Follow the implementation sequence defined in plan.md.
3. Keep the implementation appropriate for the technical assessment.
4. Avoid unnecessary architectural complexity.
5. Follow SOLID principles where appropriate.
6. Use dependency injection and repository pattern.
7. Use asynchronous methods for database and file operations.
8. Use DTOs for API request and response models.
9. Use proper HTTP status codes.
10. Never commit credentials or secrets.

## Required Reviews

Before considering a feature complete:

- Review security implications.
- Review code quality.
- Review EF Core implementation.
- Review file storage operations.
- Add or update automated tests.
- Verify error handling.

Use the relevant skills under `.github/skills/`
when performing specialized reviews.

## Important Constraints

- Maximum upload size: 10 MB.
- Do not trust user-supplied filenames.
- Prevent path traversal.
- Prevent filename collisions.
- Maintain database and file storage consistency.
- Authentication and authorization are not required.

## Final Validation

Before submission:

- Build the solution.
- Run all automated tests.
- Verify all required endpoints.
- Review README.md.
- Check that no secrets are committed.

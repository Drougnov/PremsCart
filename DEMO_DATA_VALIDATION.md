# Demo-data validation note

This package adds an **optional**, idempotent showcase-data seeder controlled by Render environment variables.

Static checks completed in the packaging environment:

- C# seeder delimiter structure checked successfully.
- 13 unique demo account emails detected.
- 41 unique product titles detected across all six seeded categories.
- Every `Find("...")` reference in the seeder resolves to one of those product titles.
- No hard-coded demo password is included; `DemoData__Password` is required when seeding is enabled.
- No shared demo Moderator/Admin account is created.
- The frontend public-image handling change was reviewed at source level.

A full .NET build could not be run in this packaging environment because the .NET SDK is not installed. A normal frontend TypeScript build also requires the project's npm dependencies, which are not installed here. Render's deployment build remains the final compile/runtime verification step.

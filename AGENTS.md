# Backend Development Guide

This guide applies to every file in this project.

- Overview: [spec.md](spec.md). Workflow: [Kidamooz backend skill](.agents/skills/kidamooz-backend/SKILL.md).
- The project uses ASP.NET Core targeting `net9.0`, EF Core 9, and SQL Server.
- Startup and middleware order are in `Program.cs`; dependency registration and startup lifecycle details are under `Infrastructure/Startup`. Controllers live in `Controllers/Admin` and `Controllers/Public`.
- Keep service logic, data access, and response contracts aligned with the existing `Services`, `Repositories`, `DTOs`, and `Mapping` structure.
- Coordinate contract changes with consumers in `../Admin` and `../android`; keep administrator and member authorization policies distinct.
- For data model changes, inspect `Data/AppDbContext.cs`, existing migrations, and `Data/DbInitializer.cs` together.
- Application startup applies migrations and seed data. Use `dotnet build back.csproj` for compilation checks; do not start the application only to verify documentation.
- Never include keys, environment files, production settings, Firebase credentials, or storage credentials in documentation or log output.
- `bin`, `obj`, and `publish` are generated. Projects under `tools` are independent and excluded from the web project compilation.
- No dedicated test project was found. A successful build is not proof of API behavior; verify relevant behavior with controlled data and a test environment.

## Mandatory Spec Kit Workflow

- Spec Kit infrastructure lives in `.specify/` and its skills live in `.agents/skills/speckit-*`. The project constitution is `.specify/memory/constitution.md` and MUST govern planning and implementation.
- Every new feature, behavior change, architectural change, or non-trivial refactor MUST complete the Spec-Driven Development workflow before implementation: `$speckit-specify`, `$speckit-clarify`, `$speckit-plan`, `$speckit-tasks`, `$speckit-analyze`, `$speckit-implement`, and `$speckit-converge`, in that order.
- Implementation MUST NOT begin until `spec.md`, `plan.md`, and `tasks.md` exist and analysis reports no unresolved blocking inconsistency. Work is not complete until convergence reports `Converged`.
- Keep feature artifacts in `specs/<feature>/`. Feed discoveries that affect requirements or design back into the same artifacts before continuing implementation.
- Every product idea, proposed capability, or materially uncertain solution MUST complete Idea Assessment before entering SDD: `$speckit-assess-intake`, `$speckit-assess-research`, `$speckit-assess-define`, `$speckit-assess-shape`, and `$speckit-assess-decide`, in that order.
- Keep assessment artifacts in `.specify/assessments/<slug>/`. Only a `go` decision with explicit scope MAY move to `$speckit-specify`. A `needs-clarification` or `kill` decision MUST stop implementation until the decision artifact is updated to `go`.
- Documentation-only edits that do not change product behavior may skip SDD. All other exceptions require an explicit user instruction recorded in the task conversation.

## Secret Safety

- Never commit or push database passwords, credential-bearing connection strings, API keys, tokens, private keys, Firebase service accounts, or app-signing keys. Do not expose them in reports, logs, documentation, or PR messages.
- Load server secrets from environment variables or approved secret storage. Use fake placeholders in publishable files. Browser and app code are not secret stores.
- Before every commit, inspect staged files and changes for secrets. Before push, inspect every outgoing commit, not only the latest diff. Reports must redact values and identify only the suspicious path and secret type.
- Do not stage real environment files, local secret settings, keys, or sensitive outputs. Avoid force-adding ignored files or staging everything without review.
- `.gitignore` does not remove tracked files or history. If a secret exists in a tracked file or earlier commit, stop the push and report it without showing the value. Removing the current line does not repair historical exposure; credential rotation and history cleanup require coordination.
- Inspect public configuration and example files too. An `example` name or absence from ignore patterns does not guarantee safety. Do not print sensitive file contents into tool output.

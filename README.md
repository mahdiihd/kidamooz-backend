# Kidamooz Backend

Kidamooz Backend is the API and operational core of the Kidamooz storytelling platform. It serves the mobile
application and administration panel, manages authentication, stores story content, coordinates AI story and
cover generation, produces narration, sends notifications, and integrates with storage and messaging providers.

## Technology

- ASP.NET Core on .NET 9
- Entity Framework Core 9 and SQL Server
- JWT authentication for administrators and members
- S3-compatible media storage through the AWS SDK
- Swagger in development
- Gemini-compatible AI APIs, Edge TTS, Firebase Cloud Messaging, and pluggable OTP providers

## Architecture

```text
Controllers/Admin/    Protected administration endpoints
Controllers/Public/   Member and public application endpoints
Services/             Application and domain workflows
Repositories/         Data-access boundaries
DTOs/                 External request and response contracts
Mapping/              Entity-to-contract mapping
Domain/Entities/      Persisted domain model
Data/                 DbContext, migrations, initialization, and data tools
Infrastructure/       Authentication, AI, storage, push, startup, and cross-cutting concerns
tools/                 Independent verification and maintenance utilities
```

## Requirements and Development

- .NET SDK 9
- SQL Server or a compatible configured database
- Provider credentials supplied through environment variables or approved secret storage
- `edge-tts` when narration generation is enabled

```bash
dotnet restore back.csproj
dotnet build back.csproj
dotnet run --project back.csproj
```

Swagger is enabled in development. Application startup applies EF Core migrations and seed logic, so use a
controlled database when running locally.

## Verification Utilities

```bash
dotnet run --project tools/MemberAuthChecks/MemberAuthChecks.csproj
dotnet run --project tools/GeminiContractChecks/GeminiContractChecks.csproj
```

These checks exercise focused contracts and do not replace end-to-end verification in a safe test environment.

## Configuration and Secrets

Configuration is loaded from appsettings files and environment overrides. Publishable configuration must use
empty values or non-secret placeholders. Never commit real database passwords, JWT secrets, API keys, Firebase
service credentials, storage credentials, or signing material.

The API is authoritative for authentication, authorization, quotas, payments, AI usage, credit balances, and
idempotency. Clients must never calculate or mutate financial state independently.

## AI and Usage Accounting

The backend sends drawings and prompts to the configured Gemini-compatible provider to generate stories and,
when requested, cover images. Provider `usageMetadata` is recorded with allowlisted counters so usage can be
audited without logging story text, drawings, credentials, or raw provider bodies.

Story text and cover generation are distinct operations. Credit reservation, final settlement, model rates,
exchange-rate protection, and reconciliation belong on the server and must remain safe under concurrent and
repeated requests.

The repository is developed with AI assistance as well. AI-generated changes are governed by project rules,
reviewable specifications, tests, and convergence checks; AI output is never treated as verified by default.

## Spec-Driven Development

This repository uses [GitHub Spec Kit](https://github.com/github/spec-kit) with the Codex integration.
Instructions are in [AGENTS.md](AGENTS.md), and governing principles are in
[the constitution](.specify/memory/constitution.md).

```text
$speckit-specify
$speckit-clarify
$speckit-plan
$speckit-tasks
$speckit-analyze
$speckit-implement
$speckit-converge
```

Feature artifacts live in `specs/<feature>/`. Implementation cannot begin before `spec.md`, `plan.md`, and
`tasks.md` exist, and is complete only after convergence reports `Converged`.

## Idea Assessment

```text
$speckit-assess-intake
$speckit-assess-research
$speckit-assess-define
$speckit-assess-shape
$speckit-assess-decide
```

Assessment artifacts live in `.specify/assessments/<slug>/`. Only a documented `go` decision with explicit
scope may proceed to feature specification.

## Security

- Keep administrator and member authorization policies separate.
- Validate all client input and scope operations to the authenticated identity.
- Use transactional and idempotent writes for identity, quota, purchase, and credit operations.
- Log only allowlisted diagnostic metadata and correlation identifiers.
- Inspect staged files and all outgoing commits for secrets before pushing.

## License

No public license has been declared in this repository.

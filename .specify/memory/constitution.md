<!--
Sync Impact Report
- Version change: template -> 1.0.0
- Added principles: Server Authority; Layered Contracts; Data Integrity; Secure Observability; Verifiable Changes
- Added sections: Platform Constraints; Spec-Driven Workflow
- Removed sections: none
- Follow-up TODOs: none
-->
# Kidamooz Backend Constitution

## Core Principles

### I. Server Authority
The API MUST be authoritative for authentication, authorization, quotas, payments, wallet balances, and all
financial calculations. Client input MUST be validated and scoped to the authenticated identity. Secrets,
credentials, raw OTPs, private media, and provider bodies MUST never appear in source control or logs.

### II. Layered Contracts
Features MUST follow the existing controller, service, repository, DTO, and mapping boundaries. Public and
admin authorization policies MUST remain distinct. Contract changes MUST be checked against both `Admin`
and `android` consumers and require a coordinated compatibility plan when breaking.

### III. Data Integrity
State changes involving identity, quotas, purchases, credits, or idempotency MUST be transactionally safe
and correct under concurrent requests. Schema changes MUST include an EF Core migration and consider startup
migration behavior, existing production data, indexes, uniqueness, rollback, and repeat execution.

### IV. Secure Observability
External calls and important state transitions MUST emit structured, correlation-friendly metadata sufficient
for diagnosis and reconciliation. Logs MUST use allowlisted fields and exclude secrets and user content.
Provider-reported usage and immutable ledger records MUST be retained when they affect billing.

### V. Verifiable Changes
Each specification MUST define acceptance scenarios for success, authorization failure, invalid input,
provider failure, and relevant concurrency boundaries. `dotnet build back.csproj` is mandatory for executable
changes. Focused contract or behavioral checks MUST be added where build success cannot prove behavior.

## Platform Constraints

- The backend uses ASP.NET Core `net9.0`, EF Core 9, and SQL Server.
- Dependencies MUST be registered with an appropriate lifetime through the existing startup structure.
- External providers MUST be accessed behind interfaces with configuration from environment variables or
  approved secret storage.
- Starting the application applies migrations; plans MUST distinguish generating a migration from applying it.

## Spec-Driven Workflow

1. Use Idea Assessment for proposals with uncertain value, cost, operational risk, or provider dependency.
2. Approved features proceed through specify, optional clarify, plan, tasks, and analyze.
3. Plans MUST cover contracts, authorization, data migration, failure behavior, observability, and clients.
4. Implementation follows the task artifact and feeds material discoveries back into the spec and plan.
5. Run converge until requirements, code, migration, and verification evidence align.

## Governance

This constitution governs Spec Kit work in the Kidamooz backend and complements `AGENTS.md`; the stricter
testable constraint applies. Amendments require a documented reason, impact review, migration implications
when relevant, and semantic version update. Every plan and review MUST verify compliance. Exceptions MUST be
explicit, narrowly scoped, and include a removal condition.

**Version**: 1.0.0 | **Ratified**: 2026-09-16 | **Last Amended**: 2026-09-16

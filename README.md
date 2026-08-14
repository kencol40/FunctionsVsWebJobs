# Functions vs WebJobs POC

A proof-of-concept exploring the differences, pros/cons, and long-term maintainability of two Azure hosting models — **Azure Functions (isolated worker)** and **Azure WebJobs SDK** — for implementing the same trigger-driven workloads:

1. **Blob-triggered CSV ingestion** — a CSV uploaded to a blob container is parsed row-by-row and persisted to SQL.
2. **Service Bus-triggered message ingestion** — a message consumed from a queue is persisted to SQL.

Both implementations share a single `FunctionsVsWebJobsPoc.Core` library (EF Core context, entities, CSV parsing, repositories, and processor services) so that only the hosting/trigger layer differs between the two stacks. The environment is orchestrated locally with **Azure Aspire** (SQL Server, Azurite storage emulator, and Service Bus emulator), and parity between the two stacks is verified with automated integration tests.

## What's in this repo

| Area | Project | Purpose |
|---|---|---|
| Shared logic | `src/FunctionsVsWebJobsPoc.Core` | EF Core `DbContext`, entities, CSV parser, repositories, and processor/handler services used by both hosts. |
| Azure Functions implementation | `src/FunctionsVsWebJobsPoc.FunctionApp` | Isolated worker Functions app with thin blob/Service Bus triggers delegating to `Core`. |
| Azure WebJobs implementation | `src/FunctionsVsWebJobsPoc.WebJobApp` | WebJobs SDK console host with thin blob/Service Bus triggers delegating to `Core`. |
| Local orchestration | `src/FunctionsVsWebJobsPoc.AppHost` | Azure Aspire AppHost wiring SQL Server, storage emulator, Service Bus emulator, and both apps together. |
| Shared service defaults | `src/FunctionsVsWebJobsPoc.ServiceDefaults` | Aspire service defaults (telemetry, health checks, resilience) shared by both apps. |
| Unit tests | `tests/FunctionsVsWebJobsPoc.Core.Tests` | Fast Moq/EF-InMemory tests for the shared `Core` library. |
| Integration/parity tests | `tests/FunctionsVsWebJobsPoc.IntegrationTests` | Aspire-hosted end-to-end tests proving identical inputs produce identical rows in both stacks' tables. |
| Manual SQL reference | `sql/init-db.sql` | Documents the `PocDb` schema (also created automatically at startup via `EnsureCreatedAsync()`). |

## Key documents

- [`Instructions.txt`](./Instructions.txt) — the original requirements/specification that this POC was built from.
- [`docs/dev-onboarding.md`](./docs/dev-onboarding.md) — prerequisites and step-by-step instructions for running the app and both test suites locally.
- [`docs/comparison-report.md`](./docs/comparison-report.md) — the Functions vs WebJobs comparison: feature/operational table, pros and cons, and a full-lifecycle maintainability analysis (onboarding, local dev, platform/runtime churn, package maintenance, deprecations, ongoing operational burden).

## Quick start

```powershell
cd Z:\source\POCs\FunctionsVsWebJobs\FunctionsVsWebJobsPoc
dotnet restore
dotnet build
dotnet test tests\FunctionsVsWebJobsPoc.Core.Tests        # fast unit tests
dotnet run --project src\FunctionsVsWebJobsPoc.AppHost    # requires Docker Desktop running
```

See [`docs/dev-onboarding.md`](./docs/dev-onboarding.md) for full details, including running the integration/parity test suite.

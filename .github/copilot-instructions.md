# Copilot Instructions

## Project Guidelines
- User prefers Moq as the mocking framework for unit tests in .NET projects.

## Reusable pattern: Azure Functions vs Azure WebJobs (or other dual-implementation) comparison POCs

When asked to build a POC that compares two ways of implementing the same Azure trigger-driven workload (e.g. Azure Functions vs Azure WebJobs), follow this structure:

- **Shared core library** (`*.Core`): put ALL business logic here — EF Core `DbContext`, entities, parsers, repositories, and processor/handler services. Both implementations under comparison must call into the exact same services via DI so that only the hosting/trigger layer differs.
- **One project per implementation being compared** (e.g. `*.FunctionApp`, `*.WebJobApp`): keep trigger entry points as thin as possible — parse/read the trigger payload, then delegate immediately to a shared `Core` service. Use a `ProcessingTarget`-style enum (or similar discriminator) so shared repositories can write to per-implementation tables/columns for parity comparison.
- **Local orchestration via Azure Aspire** (`*.AppHost`): use Aspire to spin up local emulators/containers (SQL Server, Azurite/Storage emulator, Service Bus emulator) and wire both implementation projects to the same resources via named connection strings.
- **Two test projects**:
  - `*.Core.Tests` — fast unit tests for the shared `Core` library using Moq for mocking dependencies and EF Core InMemory provider for repository tests. No Aspire/Docker dependency.
  - `*.IntegrationTests` — end-to-end parity tests using `Aspire.Hosting.Testing` (`DistributedApplicationTestingBuilder`) to start the real AppHost, submit identical inputs (same blob/message) to both implementations' resources, then poll the shared database until matching rows appear in both implementations' tables and assert equality. Requires Docker Desktop running.
- **SQL schema**: maintain a manual `sql/init-db.sql` script documenting the expected schema even though EF Core `EnsureCreatedAsync()` is called at startup by both hosts — useful as a reviewable reference and for manual troubleshooting.
- **Comparison report**: once implementations and parity tests are complete, produce a `docs/comparison-report.md` (or similar) covering: side-by-side feature/operational table, pros/cons per approach, and a maintainability discussion across the full lifecycle (developer onboarding, local dev experience, platform/runtime version changes, package/dependency maintenance, deprecations, ongoing operational burden).
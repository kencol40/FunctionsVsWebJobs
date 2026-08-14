# Developer Onboarding

This guide gets you from a clean machine to running the full POC (Function App, WebJob App, and both test suites) locally.

## 1. Prerequisites

| Requirement | Notes |
|---|---|
| **.NET 10 SDK** | Run `dotnet --version` — should report `10.0.x`. Install from https://dotnet.microsoft.com/download if missing. |
| **Docker Desktop** | Required to run local emulators (SQL Server, Azurite storage emulator, Service Bus emulator) orchestrated by Azure Aspire. Must be running before you start the AppHost or the integration tests. |
| **Azure Functions Core Tools** | Only needed if you want to run/debug `FunctionsVsWebJobsPoc.FunctionApp` directly with `func start` outside of Aspire. Install via `npm i -g azure-functions-core-tools@4 --unsafe-perm true` or the Visual Studio Functions workload. |
| **Visual Studio 2022/2026** (or VS Code with C# Dev Kit) | Solution file: `FunctionsVsWebJobsPoc/FunctionsVsWebJobsPoc.slnx`. Ensure the ASP.NET and web development, Azure development, and .NET Aspire workloads are installed if using Visual Studio. |
| **Aspire workload/tooling** | `dotnet workload install aspire` if `dotnet run` on the AppHost project reports a missing Aspire tooling error. |

No local SQL Server or Storage account install is required — Aspire provisions containerized emulators for you.

## 2. Clone and restore

```powershell
cd Z:\source\POCs\FunctionsVsWebJobs\FunctionsVsWebJobsPoc
dotnet restore
dotnet build
```

A successful build produces `Build succeeded` with 0 errors (one non-blocking `NU1510` warning from `FunctionsVsWebJobsPoc.FunctionApp` is expected and safe to ignore).

## 3. Run the full environment via Aspire (recommended)

This starts SQL Server, the Azurite storage emulator, the Service Bus emulator, the Function App, and the WebJob App together, wired to each other automatically.

1. Make sure **Docker Desktop is running**.
2. Start the AppHost:
   ```powershell
   dotnet run --project src\FunctionsVsWebJobsPoc.AppHost
   ```
   or, in Visual Studio, set `FunctionsVsWebJobsPoc.AppHost` as the startup project and press F5.
3. Open the Aspire dashboard URL printed in the console output (typically `https://localhost:17xxx`). From there you can:
   - See the status of the `sql`, `storage`, `servicebus`, `functionapp`, and `webjobapp` resources.
   - View live logs/traces for each resource.
   - Find the blob container names (`function`, `webjob`) and queue names (`function`, `webjob`) to manually upload a CSV blob or send a Service Bus message for manual testing.

To confirm both stacks are processing correctly, upload the same CSV file to both the `function` and `webjob` blob containers (or send the same message to both queues) and check that matching rows appear in the `function_blobrow_data`/`webjob_blobrow_data` (or `function_message_data`/`webjob_message_data`) tables in the `PocDb` SQL database.

## 4. Run the Function App or WebJob App standalone (optional)

You generally don't need to do this since Aspire wires up connection strings for you, but for isolated debugging:

- **Function App**:
  ```powershell
  cd src\FunctionsVsWebJobsPoc.FunctionApp
  func start
  ```
  Requires a `local.settings.json` with valid `AzureStorage` and `ServiceBusConnection` values (or an Azurite/Service Bus emulator connection string) plus a `sqldb` connection string.

- **WebJob App**:
  ```powershell
  dotnet run --project src\FunctionsVsWebJobsPoc.WebJobApp
  ```
  Requires the same configuration values via `appsettings.json`/environment variables/`local.settings.json`.

## 5. Running the tests

### Unit tests (fast, no Docker required)

```powershell
dotnet test tests\FunctionsVsWebJobsPoc.Core.Tests
```

These test the shared `Core` library in isolation using Moq for mocked dependencies and the EF Core InMemory provider for repository tests. Expected result: all tests pass in a few seconds.

### Integration / parity tests (requires Docker Desktop running)

```powershell
dotnet test tests\FunctionsVsWebJobsPoc.IntegrationTests
```

These tests use `Aspire.Hosting.Testing` to spin up the full AppHost (SQL Server, storage emulator, Service Bus emulator, Function App, WebJob App) in-process, then:

1. Upload the same CSV blob to both the `function` and `webjob` containers and poll `PocDb` until matching rows appear in `function_blobrow_data` and `webjob_blobrow_data`.
2. Send the same message to both the `function` and `webjob` queues and poll `PocDb` until matching rows appear in `function_message_data` and `webjob_message_data`.

Notes:
- First run can take longer than usual while Docker pulls the SQL Server/Azurite/Service Bus emulator images.
- If tests time out waiting for rows to appear, check the Aspire dashboard logs for the `functionapp`/`webjobapp` resources for startup or trigger errors.
- Run from Visual Studio Test Explorer or via the command above; both work the same way since the tests self-host the AppHost.

### Run everything

```powershell
dotnet build
dotnet test
```

`dotnet test` at the solution root will run both the `Core.Tests` and `IntegrationTests` projects (ensure Docker Desktop is running first).

## 6. Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| Aspire fails to start resources | Docker Desktop isn't running, or doesn't have enough resources allocated. Start Docker Desktop and retry. |
| Integration tests time out polling for rows | Check the Aspire dashboard logs for the Function/WebJob resource — a trigger may not have fired yet (cold start) or a connection string may be misconfigured. Re-run once resources report healthy. |
| `func` command not found | Install Azure Functions Core Tools (see prerequisites) or just use the Aspire AppHost instead of running the Function App standalone. |
| `NU1510` warning on `FunctionsVsWebJobsPoc.FunctionApp` | Known, non-blocking; safe to ignore. |
| EF Core schema out of date | Both hosts call `PocDbContext.Database.EnsureCreatedAsync()` at startup, so the schema is created automatically. If you need to reset it, drop the `PocDb` database (or its SQL container volume) and restart the AppHost. `sql/init-db.sql` documents the expected schema if you want to apply it manually. |

## 7. Where to look next

- `Instructions.txt` — original requirements/specification for this POC.
- `docs/comparison-report.md` — Functions vs WebJobs comparison, pros/cons, and maintainability analysis produced from this POC.
- `.github/copilot-instructions.md` — reusable pattern notes for building similar dual-implementation comparison POCs.

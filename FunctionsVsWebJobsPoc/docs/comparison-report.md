# Azure Functions vs Azure WebJobs — Comparison Report

## 1. Overview

This POC implements the exact same two scenarios twice, once as an **Azure Functions (isolated worker)** app and once as a classic **Azure WebJobs SDK** console host:

1. **Blob-triggered CSV ingestion** — a CSV file uploaded to a blob container is parsed row-by-row and each row is written as a JSON document to a SQL table.
2. **Service Bus-triggered message ingestion** — a message on a queue is consumed and its body is written as JSON to a SQL table.

Both stacks share the same `FunctionsVsWebJobsPoc.Core` library: the same `PocDbContext` (EF Core), the same CSV parser, the same `IBlobRowProcessor` / `IServiceBusMessageProcessor` handler services, and the same repositories. Only the **trigger entry points** differ — a few lines of attribute-decorated methods in each host. This isolates the comparison to exactly the thing being compared: the hosting/trigger model, not the business logic.

| | Azure Functions (isolated worker) | Azure WebJobs SDK |
|---|---|---|
| Project type | `Microsoft.Azure.Functions.Worker` isolated worker (out-of-process) | Plain .NET console app hosting a `JobHost` via the Generic Host |
| Blob trigger | `[BlobTrigger("function/{name}")]` on a method | `[BlobTrigger("webjob/{name}")]` on a method (same attribute, different package) |
| Service Bus trigger | `[ServiceBusTrigger("function")]` | `[ServiceBusTrigger("webjob")]` |
| Hosting | `FunctionsApplication.CreateBuilder` + Azure Functions host process (`func.exe` locally, Functions runtime in Azure) | `HostBuilder().ConfigureWebJobs(...)` — an ordinary console app process |
| DI container | Standard `IServiceCollection` via `builder.Services` | Standard `IServiceCollection` via `ConfigureServices` |
| Local dev / debug | Requires Azure Functions Core Tools / `func start`, `local.settings.json`, a storage account (or Azurite) for the internal "AzureWebJobsStorage" | Just `dotnet run` — no special local tooling, only the same storage account for locks/leases |
| Deployment target | Azure Functions (Consumption, Premium, Flex Consumption, App Service plan) | Azure App Service "WebJobs" feature, or any container/VM/App Service that can run a long-lived console process |
| Scaling | Automatic, trigger-based scale controller (Functions runtime) | Tied to the scaling of whatever App Service Plan / container hosts it — no automatic per-trigger scale-out |
| Cold start | Consumption plan can cold start; Premium/Flex avoids it | None — it's just a long running process |

Confirms the "Functions is built on WebJobs" claim in practice: both use the *same* WebJobs SDK trigger attributes (`[BlobTrigger]`, `[ServiceBusTrigger]`) and, under the hood, the Functions host is itself a specialised WebJobs host with the scale controller and packaging model layered on top.

## 2. Code size / additional ceremony

Roughly, on top of the shared `Core` project:

- **Function App**: 2 trigger classes (~25 lines each), `Program.cs` (~30 lines), `host.json`, `local.settings.json`, plus generated `WorkerExtensions` project and several NuGet packages tied to the isolated worker model (`Microsoft.Azure.Functions.Worker*`).
- **WebJob App**: 2 trigger classes (~25 lines each, functionally identical), `Program.cs` (~35 lines, slightly more because DI/host wiring is manual instead of templated), `local.settings.json`. No generated extensions project.

**Net result**: very close to a wash in terms of lines of code for *this* scenario, because the heavy lifting (CSV parsing, EF Core persistence) lives in `Core` either way. The meaningful difference is operational, not code volume.

## 3. Pros and Cons

### Azure Functions

**Pros**
- Built-in automatic scaling per trigger (Consumption/Premium/Flex Consumption plans).
- First-class binding ecosystem (blob, queue, service bus, timers, HTTP, Event Grid, Durable Functions, etc.) with less manual wiring for many binding types.
- Strong tooling/story in Visual Studio, Azure Portal monitoring, Application Insights integration out of the box.
- Pay-per-execution billing model on Consumption plan can be cheaper for spiky/low-volume workloads.

**Cons**
- Extra moving parts: isolated worker process, `host.json`, `local.settings.json`, generated `WorkerExtensions` project, Azure Functions Core Tools for local development.
- Runtime/model churn: the move from in-process to isolated worker model (and now the newer "worker extensions" packaging) has forced real migration work for existing customers, exactly the kind of platform-driven maintenance cost the business discussion is worried about.
- Cold starts on Consumption plan can affect latency-sensitive scenarios.
- Slightly higher lock-in to Azure Functions-specific concepts (bindings, `host.json` config, trigger scale controller behaviour) which can be harder to reason about/debug than a plain console app.

### Azure WebJobs

**Pros**
- It's just a .NET console app / Generic Host — anyone who knows `IHost`, `IServiceCollection`, and `HostBuilder` already knows how to run and debug it. No special CLI tooling needed locally (just `dotnet run`).
- Simpler mental model: fewer moving parts, no generated extension projects, no separate worker process.
- Easier to unit test the host wiring itself since it's plain C#/Generic Host code.
- Because it's "just a process", it is trivial to host anywhere a .NET console app can run (App Service WebJob, container, VM, Kubernetes CronJob/Deployment) without being tied to the Functions runtime.

**Cons**
- No automatic per-trigger scaling — scaling is whatever the hosting compute (App Service plan, container replicas) provides. For bursty workloads this is a real operational disadvantage vs. Functions Consumption/Premium.
- Fewer "batteries included" binding types and less polished portal-level monitoring/dashboard experience than Functions.
- The WebJobs SDK is a mature, comparatively slow-moving project — this can be read as a pro (stability) or a con (fewer new features, e.g. some newer binding extensions target Functions first).
- Continuous WebJobs specifically require an "Always On" App Service plan (cost implication) to guarantee the process keeps running.

## 4. Maintainability — full lifecycle view

| Lifecycle stage | Azure Functions | Azure WebJobs |
|---|---|---|
| **Developer onboarding** | Needs to learn Functions-specific concepts: bindings, `host.json`, isolated worker model, Azure Functions Core Tools, `local.settings.json`. Steeper initial learning curve. | Needs to know Generic Host / `IHost` — already common .NET knowledge. Faster onboarding for teams already comfortable with ASP.NET Core-style hosting. |
| **Local development/debugging** | Requires Core Tools installed and kept in sync with the Functions runtime version; occasional friction when Core Tools/runtime versions drift from the SDK. | `dotnet run`/F5 debugging — no separate CLI/runtime to keep in sync. |
| **Platform/runtime changes** | History shows real churn: v1→v2→v3→v4 host changes, and the in-process → isolated worker migration (in-process model is now on a deprecation path). Each of these has required code changes, package upgrades, and sometimes behavioural differences (e.g. binding redirects, middleware pipeline changes). | The WebJobs SDK (`Microsoft.Azure.WebJobs`) has been comparatively stable for years; changes tend to be additive (new binding extensions) rather than breaking model changes. |
| **.NET version upgrades** | Isolated worker model decouples the worker's TFM from the host's, which *helps* — but you still depend on Microsoft shipping updated `Microsoft.Azure.Functions.Worker*` packages promptly for each new .NET version (as observed while building this POC, .NET 10 support already existed for Worker/Worker.Sdk, which is a good sign, but this has not always been true on day one of previous .NET releases). | Being a plain console app, it moves at the same pace as the rest of the .NET ecosystem — no separate host runtime compatibility matrix to track. |
| **Package maintenance / security & deprecation** | More Functions-specific packages to track (`Microsoft.Azure.Functions.Worker`, `.Worker.Sdk`, `.Worker.Extensions.*`, `.Worker.OpenTelemetry`, generated `WorkerExtensions` project), each with its own release cadence and occasional breaking changes/deprecations. | Fewer, more general-purpose packages (`Microsoft.Azure.WebJobs`, `.Extensions.Storage`, `.Extensions.ServiceBus`) that overlap heavily with packages a normal worker-service app would already use. |
| **Ongoing operational maintainability** | Portal-level monitoring, scaling, and Application Insights integration reduce day-2 operational burden once set up. | Operationally it's "just another App Service/website/container" to monitor — teams already running other .NET services can reuse existing observability/ops tooling with minimal Functions-specific knowledge. |

### AI-assisted overall assessment

For a team that:
- already runs other ASP.NET Core / worker-service style .NET applications, and
- has predictable (rather than extremely bursty) load, and
- values minimising platform-specific churn and onboarding friction,

**WebJobs presents a genuinely lower long-term maintenance burden** — it rides on the general .NET/Generic Host ecosystem rather than a Functions-specific runtime and packaging model that has changed significantly more than once (v1→v4, in-process→isolated).

For a team that:
- needs true per-trigger elastic scale-to-zero economics, or
- wants the richest out-of-the-box binding/monitoring ecosystem, or
- is comfortable absorbing occasional Functions-runtime migration work in exchange for that scaling story,

**Azure Functions remains the stronger choice**, and the "Functions is rubbish" framing understates how much of the binding/trigger plumbing is genuinely shared with WebJobs (this POC demonstrates that directly — the trigger method signatures are nearly identical).

**Bottom line**: the "Functions add long-term maintenance cost" claim has real evidence behind it (isolated worker migration, additional Functions-specific packages/tooling, Core Tools/runtime version coupling), but it is a trade-off against automatic scaling and a richer managed platform experience — not a clear-cut "WebJobs is always better" conclusion. The right choice depends on whether the workload's scaling profile justifies the extra platform surface area.

## 5. How this POC demonstrates it

- `src/FunctionsVsWebJobsPoc.FunctionApp/BlobTriggerFunction.cs` vs `src/FunctionsVsWebJobsPoc.WebJobApp/BlobJobFunctions.cs` — near-identical trigger method bodies.
- `src/FunctionsVsWebJobsPoc.FunctionApp/ServiceBusTriggerFunction.cs` vs `src/FunctionsVsWebJobsPoc.WebJobApp/ServiceBusJobFunctions.cs` — same pattern.
- `tests/FunctionsVsWebJobsPoc.IntegrationTests/ParityTests.cs` — proves both stacks produce identical persisted data for identical inputs.
- `src/FunctionsVsWebJobsPoc.Core` — the shared business logic that neither stack needs to duplicate.

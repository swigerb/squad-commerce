# Architecture Review Report — Phase 6 Final Quality Gate

> **Reviewer:** Bill Gates (Lead)  
> **Date:** 2026-03-24  
> **Scope:** Full source code review against `.squad/architecture.md`  
> **Verdict:** 🟡 **Solid foundation — integration wiring incomplete**

---

## Executive Summary

The team has built an impressive amount of production-quality code. The individual project implementations — Contracts, Agents, MCP, A2A, API, Web, ServiceDefaults — are each well-crafted with proper records, DI, telemetry, and error handling. However, **the projects are not yet wired together at the API layer**, meaning the full end-to-end scenario cannot execute. The pieces are all there; they just need to be connected.

---

## 1. Architecture Compliance

### ✅ Items That Match the Architecture

| # | Area | Finding |
|---|------|---------|
| 1 | **Solution Structure** | All 8 src projects exist exactly as specified: `AppHost`, `ServiceDefaults`, `Api`, `Agents`, `Mcp`, `A2A`, `Contracts`, `Web` |
| 2 | **Contracts Zero-Dep** | `SquadCommerce.Contracts.csproj` has zero project references and zero NuGet packages — exactly as required |
| 3 | **Agent Naming** | 4 agents match: `ChiefSoftwareArchitectAgent`, `InventoryAgent`, `PricingAgent`, `MarketIntelAgent` |
| 4 | **AgentPolicy Record** | Immutable `sealed record AgentPolicy` with all 6 fields: `AgentName`, `EnforceA2UI`, `RequireTelemetryTrace`, `PreferredProtocol`, `AllowedTools`, `EntraIdScope` — **exact match** (`Agents\Policies\AgentPolicy.cs`) |
| 5 | **Policy Table** | All 4 policies in `AgentPolicyRegistry` match the architecture doc table exactly — scopes, tools, protocols all correct (`Agents\Policies\AgentPolicyRegistry.cs`) |
| 6 | **Orchestrator Delegates Only** | `ChiefSoftwareArchitectAgent` has `AllowedTools = []` and explicitly documents "NEVER calls MCP tools directly" — **architectural constraint enforced** (`Agents\Orchestrator\ChiefSoftwareArchitectAgent.cs:13-19`) |
| 7 | **MCP Tools** | Both tools implemented: `GetInventoryLevelsTool`, `UpdateStorePricingTool` with structured error payloads (`Mcp\Tools\`) |
| 8 | **ConcurrentDictionary** | Both `InventoryRepository` and `PricingRepository` use `ConcurrentDictionary<string, T>` for thread safety (`Mcp\Data\`) |
| 9 | **Demo Data** | 5 stores × 8 SKUs = 40 records in both repositories — realistic data as specified |
| 10 | **A2A Components** | `AgentCard`, `A2AClient`, `A2AServer`, `ExternalDataValidator` all present (`A2A\`) |
| 11 | **A2A Validation** | `ExternalDataValidator` implements 4-tier confidence (High/Medium/Low/Unverified), only surfaces High+Medium — **Zero Trust enforced** (`A2A\Validation\ExternalDataValidator.cs`) |
| 12 | **External Data Never Raw** | `MarketIntelAgent` filters to `ConfidenceLevel is "High" or "Medium"` before building A2UI payload (`Agents\Domain\MarketIntelAgent.cs:93-97`) |
| 13 | **A2UI Components** | All 3 Blazor components exist: `RetailStockHeatmap.razor`, `PricingImpactChart.razor`, `MarketComparisonGrid.razor` (`Web\Components\A2UI\`) |
| 14 | **A2UIPayload Record** | `A2UIPayload` record with `Type`, `RenderAs` discriminator, `Data` — matches spec (`Contracts\A2UI\A2UIPayload.cs`) |
| 15 | **A2UIRenderer Dispatcher** | Routes `RenderAs` to correct component with fallback for unknown types (`Web\Components\A2UI\A2UIRenderer.razor`) |
| 16 | **AG-UI Endpoint** | `MapGet("/api/agui")` with SSE headers (`text/event-stream`, `no-cache`, `keep-alive`) — matches spec (`Api\Program.cs:51-75`) |
| 17 | **SignalR Hub** | `AgentHub` at `/hubs/agent` with session-based groups — matches spec (`Api\Hubs\AgentHub.cs`) |
| 18 | **AG-UI Events** | 5 event types: `text_delta`, `tool_call`, `status_update`, `a2ui_payload`, `done` — matches spec |
| 19 | **Channel-Based Streaming** | `AgUiStreamWriter` uses `ConcurrentDictionary<string, Channel<AgUiEvent>>` — matches D1 decision (`Api\Services\AgUiStreamWriter.cs:12`) |
| 20 | **CORS with Credentials** | `.AllowCredentials()` explicitly configured for SignalR — matches D8 decision (`Api\Program.cs:33`) |
| 21 | **TypedResults** | All endpoints return `Ok<T>`, `Accepted<T>`, `NotFound` — matches D5 decision |
| 22 | **Structured Records for DTOs** | All models, requests, and responses are `sealed record` with `required` + `init` properties |
| 23 | **Entra ID Scopes** | 4 scopes match exactly: `Orchestrate`, `Inventory.Read`, `Pricing.ReadWrite`, `MarketIntel.Read` (`Api\Middleware\EntraIdScopeMiddleware.cs:16-22`) |
| 24 | **Demo Mode** | `EntraIdScopeMiddleware` supports `EnforcementMode = "Demo"` — matches D3 decision |
| 25 | **Custom Metrics** | All 8 metrics match architecture doc exactly: `squad.agent.invocation.count/duration`, `squad.mcp.tool.call.count/duration`, `squad.a2a.handshake.count/duration`, `squad.a2ui.payload.count`, `squad.pricing.decision.count` (`ServiceDefaults\SquadCommerceMetrics.cs`) |
| 26 | **Activity Sources** | 4 sources registered: `SquadCommerce.Agents`, `.Mcp`, `.A2A`, `.AgUi` (`ServiceDefaults\SquadCommerceTelemetry.cs`) |
| 27 | **Constructor Injection** | All agents and services use constructor DI with null checks |
| 28 | **Graceful Degradation** | Orchestrator continues if `InventoryAgent` fails (`ChiefSoftwareArchitectAgent.cs:96-97`) — matches D7 decision |
| 29 | **4 Pricing Scenarios** | Current, Match Competitor, Beat by 5%, Split Difference — matches D9 decision (`Agents\Domain\PricingAgent.cs:115-128`) |
| 30 | **Mock A2A Data** | 3 competitors: TechMart (-8%), ElectroWorld (-5%), GadgetZone (+3%) — matches D8 decision (`A2A\A2AClient.cs:183-215`) |
| 31 | **Pricing Endpoints** | `/api/pricing/approve`, `/api/pricing/reject`, `/api/pricing/modify` — matches spec (`Api\Endpoints\PricingEndpoints.cs`) |
| 32 | **Agent Endpoints** | `/api/agents/`, `/api/agents/{name}/status`, `/api/agents/analyze` (`Api\Endpoints\AgentEndpoints.cs`) |
| 33 | **Blazor Layout** | Fixed sidebar (400px) for chat, flexible right panel — matches D3 frontend decision |
| 34 | **ApprovalPanel** | Three-button workflow (Approve, Modify, Reject) with confirmation — matches D4 frontend decision (`Web\Components\Chat\ApprovalPanel.razor`) |
| 35 | **SSE Client** | `AgUiStreamService` parses `text_delta`, `status_update`, `a2ui_payload`, `tool_call`, `done` — matches D7 frontend decision |
| 36 | **SignalR Client** | `SignalRStateService` with auto-reconnect and exponential backoff — matches D8 frontend decision |
| 37 | **OpenTelemetry Spans** | Every agent emits spans via `SquadCommerceTelemetry.StartAgentSpan()` with proper tags — full observability |
| 38 | **Aspire Orchestration** | `AppHost.cs` wires `api` and `web` with `WithReference(api).WaitFor(api)` |
| 39 | **Test Projects** | 5 test projects exist: `Agents.Tests`, `Mcp.Tests`, `A2A.Tests`, `Integration.Tests`, `Web.Tests` |

---

## 2. Deviations

### ⚠️ Interface Signature Mismatches vs Architecture Doc

| # | Area | Architecture Doc Says | Actual Implementation | File:Line |
|---|------|----------------------|----------------------|-----------|
| 1 | **IDomainAgent** | `Name` property, `Policy` property, `ExecuteAsync(AgentContext, CancellationToken)` | Only `AgentName` property. No `Policy` or `ExecuteAsync` on interface. Agents have custom `ExecuteAsync` signatures. | `Agents\IDomainAgent.cs:7-13` |
| 2 | **AgentResult** | `TextResponse`, `UIPayloads: IReadOnlyList<A2UIPayload>` | `TextSummary`, `A2UIPayload: object?` (single, not list) | `Agents\IDomainAgent.cs:18-45` |
| 3 | **IA2AClient** | `SendTaskAsync(string externalAgentUrl, A2ATaskRequest request, CancellationToken ct)` | `GetCompetitorPricingAsync(string sku, ct)` + `ValidateExternalDataAsync(CompetitorPricing, ct)` | `Contracts\Interfaces\IA2AClient.cs:6-9` |
| 4 | **IInventoryRepository** | `GetLevelsAsync(string[] storeIds, string[] skuIds, ct)` | `GetInventoryLevelsAsync(string sku, ct)` + `GetInventoryForStoreAsync(string storeId, string sku, ct)` | `Contracts\Interfaces\IInventoryRepository.cs:6-9` |
| 5 | **IPricingRepository** | `UpdatePricingAsync(string storeId, IReadOnlyList<PriceChange> changes, ct)` | `UpdatePricingAsync(PriceChange change, ct)` + `GetCurrentPriceAsync(string storeId, string sku, ct)` | `Contracts\Interfaces\IPricingRepository.cs:6-9` |
| 6 | **A2UIPayload** | `sealed record` | `record` (not sealed) | `Contracts\A2UI\A2UIPayload.cs:3` |

**Assessment:** These are deliberate simplifications that make the code more practical. The interfaces evolved from the spec to better fit the demo scenario. The architecture doc should be updated to match, or the interfaces aligned — either direction is fine.

### ⚠️ Structural Deviations

| # | Finding | File |
|---|---------|------|
| 7 | **AppHost entry point** is `AppHost.cs`, not `Program.cs` as architecture doc specifies | `AppHost\AppHost.cs` |
| 8 | **Duplicate telemetry classes:** Both `SquadCommerceTelemetry` (static) and `SquadCommerceMetrics` (singleton) exist in ServiceDefaults with overlapping ActivitySources and metrics. This creates two separate ActivitySource instances per name, which could confuse trace correlation. | `ServiceDefaults\SquadCommerceTelemetry.cs` + `ServiceDefaults\SquadCommerceMetrics.cs` |
| 9 | **PricingAgent casts to concrete type:** `if (_pricingRepository is Mcp.Data.PricingRepository repo)` violates interface abstraction. Domain agent should not know about MCP internals. | `Agents\Domain\PricingAgent.cs:84` |
| 10 | **Api.csproj missing project references:** Only references `ServiceDefaults` and `Contracts`. Does not reference `Agents`, `Mcp`, or `A2A` — meaning agent execution cannot be wired up. | `Api\SquadCommerce.Api.csproj:3-6` |
| 11 | **Web.csproj missing ServiceDefaults reference:** Doesn't reference ServiceDefaults — no shared OTel config in the frontend. | `Web\SquadCommerce.Web.csproj` |

---

## 3. Missing Items

### ❌ Critical — Blocks End-to-End Scenario

| # | Missing Item | Impact |
|---|-------------|--------|
| 1 | **Agent DI not wired in API** | `AddSquadCommerceAgents()` is never called in `Api\Program.cs`. Agents, policies, and workflow are not registered in the DI container. |
| 2 | **MCP DI not wired in API** | `AddSquadCommerceMcp()` is never called in `Api\Program.cs`. Repositories and MCP tools are not registered. |
| 3 | **A2A DI not wired in API** | No `A2AClient` HttpClient factory registration. `IA2AClient` has no DI binding. |
| 4 | **TriggerAnalysis is mock-only** | `AgentEndpoints.TriggerAnalysis()` simulates status messages with `Task.Delay` instead of calling `ChiefSoftwareArchitectAgent.ProcessCompetitorPriceDropAsync()` — no real agent orchestration (`Api\Endpoints\AgentEndpoints.cs:108-131`) |
| 5 | **A2AServer handlers are stubs** | All 3 `HandleGet*` methods return `"Stub: pending"` with TODO comments (`A2A\A2AServer.cs:54-124`) |

### ❌ Important — Missing from Architecture Spec

| # | Missing Item | Architecture Doc Reference |
|---|-------------|---------------------------|
| 6 | **RetailWorkflow not integrated** | `RetailWorkflow.cs` is a stub with empty methods. Architecture says "MAF Graph-based Workflow" should coordinate orchestration. Currently, `ChiefSoftwareArchitectAgent` does sequential calls instead. |
| 7 | **AgentPolicyRegistry not registered in DI** | Policies are defined but the `AddSquadCommerceAgents()` extension has the registration code commented out (`Agents\Registration\AgentServiceExtensions.cs:47-52`) |
| 8 | **No A2A endpoint for incoming requests** | Architecture specifies external agents can discover our Agent Cards, but no `/a2a/*` endpoints are mapped in `Api\Program.cs`. `A2AServer.HandleRequest` is never invoked. |
| 9 | **No Agent Card discovery endpoint** | Architecture Section 3.2 specifies Agent Card publishing for A2A discovery. No endpoint serves Agent Cards. |

---

## 4. Code Quality Assessment

### ✅ Excellent

- **Records everywhere** — All DTOs, payloads, policies, events, requests, and responses are immutable records with `required` + `init`
- **Null guards** — Every constructor validates parameters with `?? throw new ArgumentNullException`
- **Structured logging** — Consistent use of `ILogger<T>` with structured parameters throughout
- **Error handling** — Try/catch at agent boundaries with proper span error status and duration recording
- **XML documentation** — Thorough doc comments on all public APIs
- **Separation of concerns** — Clean layers: Contracts → MCP/A2A → Agents → API → Web

### ⚠️ Needs Attention

- **Duplicate telemetry classes** — `SquadCommerceTelemetry` (static) vs `SquadCommerceMetrics` (singleton). Consolidate to one pattern.
- **Concrete type cast** — `PricingAgent` casting `IPricingRepository` to `Mcp.Data.PricingRepository` for `GetCost()`. Add `GetCostAsync()` to the interface instead.
- **Hardcoded store IDs** — Store IDs `SEA-001` through `DEN-005` appear in multiple files. Extract to a shared constants class.

---

## 5. README Accuracy

### ⚠️ README Deviations from Actual Implementation

| # | README Says | Actual |
|---|-------------|--------|
| 1 | Project listed as `SquadCommerce.Shared` | Actual project is `SquadCommerce.Contracts` |
| 2 | Test projects include `SquadCommerce.Web.Tests` only | Also has `SquadCommerce.Integration.Tests` |
| 3 | "Database: SQL Server (via MCP tools)" | Actual: In-memory `ConcurrentDictionary` (no SQL Server) |
| 4 | Code example shows `AddAgent<ChiefSoftwareArchitect>` | This MAF API doesn't exist yet; agents registered as `AddScoped<T>()` |
| 5 | Code example shows `options.Policy = commercePolicy` | Policies registered via `AgentPolicyRegistry`, not `options.Policy` |
| 6 | "Prerequisites: SQL Server" | Not required — all data is in-memory |

---

## 6. Protocol Compliance

### AG-UI ✅
- SSE endpoint at `/api/agui` with proper headers
- 5 event types implemented
- Channel-based pub/sub with `ConcurrentDictionary<string, Channel<AgUiEvent>>`
- `ToSseFormat()` produces correct `data: {json}\n\n` format

### A2UI ✅
- 3 components with `RenderAs` discriminator routing
- `A2UIRenderer.razor` dispatcher with fallback
- All components parse `JsonElement` from payload data
- Rich visualizations with color coding and accessibility

### A2A ⚠️
- Client side: Mock implementation works for demo
- Server side: All handlers are stubs (TODO comments)
- Validation: Fully implemented with 4-tier confidence scoring
- Missing: No A2A endpoints in API, no Agent Card discovery

### MCP ✅
- Tools implemented with structured error payloads
- Repositories with thread-safe ConcurrentDictionary
- Schema definitions for tool discovery
- Realistic demo data

---

## 7. Security Assessment

### ✅ Good
- Entra ID middleware with scope-per-agent mapping
- Demo mode for development, strict mode for production
- Scope extraction from JWT `scope` and `scopes` claims
- Health check endpoints excluded from scope validation

### ⚠️ Needs Work
- No JWT validation configured (no `AddAuthentication()`, `AddMicrosoftIdentityWebApi()` in `Program.cs`)
- No authorization attributes on endpoints
- CORS allows `AllowAnyHeader()` + `AllowAnyMethod()` — should be tightened for production

---

## 8. Observability Assessment

### ✅ Excellent
- 4 custom ActivitySources registered in OpenTelemetry
- 8 custom metrics matching architecture doc exactly
- All 3 domain agents emit invocation spans with proper tags
- Orchestrator creates parent span wrapping full workflow
- MCP tool calls have dedicated span helpers
- Error status set on spans with error type and message
- Duration recorded even on failures
- Structured logging with trace context

### ⚠️ Minor
- Duplicate `ActivitySource` instances from both `SquadCommerceTelemetry` (static) and `SquadCommerceMetrics` (DI singleton)
- `A2AClient` doesn't emit A2A handshake spans (uses mock path)

---

## 📋 Recommendations for Polish

### Priority 1 — Wire It Together
1. Add `Agents`, `Mcp`, `A2A` project references to `Api.csproj`
2. Call `AddSquadCommerceMcp()` and `AddSquadCommerceAgents()` in `Api\Program.cs`
3. Register `IA2AClient` → `A2AClient` with `AddHttpClient<IA2AClient, A2AClient>()`
4. Replace mock `TriggerAnalysis` with real `ChiefSoftwareArchitectAgent.ProcessCompetitorPriceDropAsync()`
5. Emit A2UI payloads via `IAgUiStreamWriter.WriteA2UIPayloadAsync()` from real agent results

### Priority 2 — Consolidate & Clean
6. Consolidate `SquadCommerceTelemetry` (static) and `SquadCommerceMetrics` (singleton) into one approach
7. Add `GetCostAsync(string storeId, string sku)` to `IPricingRepository` — remove concrete cast in PricingAgent
8. Extract hardcoded store IDs into `SquadCommerce.Contracts.Constants.StoreIds`
9. Update architecture doc interfaces to match actual implementations (or vice versa)

### Priority 3 — README & Polish
10. Fix README: `SquadCommerce.Shared` → `SquadCommerce.Contracts`
11. Fix README: Remove SQL Server prerequisite
12. Fix README: Update code example to match actual DI pattern
13. Add `SquadCommerce.Integration.Tests` to README project structure
14. Rename `AppHost.cs` to `Program.cs` to match architecture doc convention

### Priority 4 — A2A Server
15. Wire `A2AServer` handlers to real agent execution
16. Map A2A endpoints in `Api\Program.cs` (e.g., `app.MapPost("/a2a/{capability}", ...)`)
17. Add Agent Card discovery endpoint (`GET /.well-known/agent-card`)

---

## Verdict

**The architecture is sound. The implementation is thorough. The gap is integration wiring.**

Every individual project does exactly what the architecture document says. The Contracts are clean. The Agents have real business logic with full telemetry. The MCP tools have real data with thread-safe repositories. The A2A client has realistic mock data with genuine validation. The Web frontend has beautiful A2UI components with accessibility. The API has proper SSE streaming, SignalR, and CORS.

But `Api\Program.cs` doesn't call `AddSquadCommerceAgents()` or `AddSquadCommerceMcp()`, and the `TriggerAnalysis` endpoint runs a simulated workflow instead of the real orchestrator. **Fix those 4-5 wiring lines and this system works end-to-end.**

Score: **8.5/10** — Exceptional individual components, pending final integration.

---

*Review completed by Bill Gates (Lead) — 2026-03-24*

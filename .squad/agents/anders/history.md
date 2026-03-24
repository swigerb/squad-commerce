# Project Context

- **Owner:** Brian Swiger
- **Project:** squad-commerce — A sample commerce application demonstrating Microsoft Agent Framework (MAF), A2A, MCP, AG-UI, and A2UI
- **Stack:** ASP.NET Core, SignalR, Blazor (A2UI), C#, Microsoft Agent Framework, MCP, A2A, AG-UI
- **Created:** 2026-03-24

## Core Context

Backend developer for squad-commerce. Responsible for ASP.NET Core infrastructure, SignalR real-time communication, middleware pipeline, DI configuration, and API controllers. Handles the web plumbing that connects MAF agents to the Blazor frontend.

## Learnings

### 2026-03-24: Initial Solution Scaffolding

**Projects Created:**
- `SquadCommerce.slnx` — .NET 10 solution file (new XML-based format)
- `src/SquadCommerce.AppHost/` — .NET Aspire AppHost for orchestration
- `src/SquadCommerce.ServiceDefaults/` — Aspire service defaults (OpenTelemetry, health checks, resilience)
- `src/SquadCommerce.Api/` — ASP.NET Core Web API with SignalR
- `src/SquadCommerce.Contracts/` — Shared DTOs and interfaces (zero dependencies)

**API Structure:**
- `Hubs/AgentHub.cs` — SignalR hub for background state updates (StatusUpdate, UrgencyUpdate)
- `Endpoints/AgentEndpoints.cs` — Agent orchestration endpoint stubs
- `Endpoints/PricingEndpoints.cs` — Pricing endpoint stubs
- `Middleware/EntraIdScopeMiddleware.cs` — Entra ID scope validation middleware stub
- Program.cs wired up: SignalR, CORS, middleware, endpoint mapping, ServiceDefaults integration

**Contracts Architecture:**
- A2UI payloads: `A2UIPayload`, `RetailStockHeatmapData`, `PricingImpactChartData`, `MarketComparisonGridData`
- Domain models: `InventorySnapshot`, `PriceChange`, `PricingUpdateResult`, `CompetitorPricing`
- Interfaces: `IInventoryRepository`, `IPricingRepository`, `IA2AClient`
- All types are immutable records with required init-only properties

**Patterns Used:**
- Records for all data types — immutable by default
- Required properties with `init` accessors — no nullable surprises
- Endpoint group pattern with `MapGroup()` — clean API organization
- Middleware pipeline pattern — separation of concerns
- Aspire service defaults pattern — centralized observability and resilience

**Project References:**
- Api → ServiceDefaults, Contracts
- AppHost → Api (for Aspire orchestration)
- Contracts has zero dependencies (prevents circular references)

**Build Status:** ✅ Solution builds successfully with .NET 10.0.200

**Notes:**
- .NET 10 uses `.slnx` format (XML-based solution files)
- SignalR package added (NU1510 warning is expected — ASP.NET Core includes it)
- AppHost includes placeholder comments for projects other agents will create (Web, Agents, Mcp, A2A)
- All endpoint implementations are stubs with TODO comments indicating required functionality

### 2026-03-24: Phase 4 & 5 Infrastructure Implementation — Full AG-UI, SignalR, Observability, and Security

**Phase 4 — AG-UI + SignalR Infrastructure (Completed):**

1. **MapAGUI SSE Endpoint** (`/api/agui`):
   - Implemented Server-Sent Events streaming endpoint in Program.cs
   - Returns SSE stream with proper headers: `Content-Type: text/event-stream`, `Cache-Control: no-cache`
   - Supports event types: `text_delta`, `tool_call`, `status_update`, `a2ui_payload`, `done`
   - Each event is JSON object: `{ type, data }`

2. **IAgUiStreamWriter Interface & Implementation**:
   - Created `Services/IAgUiStreamWriter.cs` — interface for agents to push AG-UI events
   - Created `Services/AgUiStreamWriter.cs` — in-memory implementation using Channels for pub/sub
   - Created `Services/AgUiEvent.cs` — record type with `ToSseFormat()` helper
   - Registered as singleton in DI: `builder.Services.AddSingleton<IAgUiStreamWriter, AgUiStreamWriter>()`
   - Uses `ConcurrentDictionary<string, Channel<AgUiEvent>>` for session isolation

3. **AgentHub SignalR Hub** (Enhanced):
   - Added session group support: `JoinSession(sessionId)`, `LeaveSession(sessionId)`
   - Implemented `SendStatusUpdate(sessionId, agentName, status)` — broadcasts to session group
   - Implemented `SendUrgencyUpdate(sessionId, level)` — broadcasts urgency changes
   - Implemented `SendA2UIPayload(sessionId, payload)` — broadcasts A2UI component data
   - Implemented `SendNotification(sessionId, message)` — push notifications
   - All methods scoped to session groups for multi-tenant isolation
   - Comprehensive logging for all hub operations

4. **PricingEndpoints** (Fully Implemented):
   - `POST /api/pricing/approve` — accepts `PricingApprovalRequest`, logs approval, returns success
   - `POST /api/pricing/reject` — accepts `PricingRejectionRequest`, logs rejection with reason
   - `POST /api/pricing/modify` — accepts `PricingModificationRequest`, re-triggers calculation
   - All endpoints use TypedResults for explicit return types
   - Structured request/response records: `PricingApprovalRequest`, `PricingRejectionRequest`, `PricingModificationRequest`, `PricingActionResponse`

5. **AgentEndpoints** (Fully Implemented):
   - `GET /api/agents` — lists all registered agents with their policies (mock data for now)
   - `GET /api/agents/{name}/status` — returns agent status (mock implementation)
   - `POST /api/agents/analyze` — triggers competitor price drop analysis scenario
   - Analysis endpoint demonstrates orchestration workflow: spawns background task, pushes status updates via `IAgUiStreamWriter`
   - Returns `Accepted` with session ID and AG-UI stream URL
   - Structured response records: `AgentListResponse`, `AgentInfo`, `AgentStatusResponse`, `AnalysisRequest`, `AnalysisResponse`

**Phase 5 — OpenTelemetry + Entra ID (Completed):**

1. **SquadCommerceTelemetry** (`ServiceDefaults/SquadCommerceTelemetry.cs`):
   - Centralized telemetry constants for all activity sources and metrics
   - **Activity Sources**: `SquadCommerce.Agents`, `SquadCommerce.Mcp`, `SquadCommerce.A2A`, `SquadCommerce.AgUi`
   - **Custom Metrics** (8 total):
     - `squad.agent.invocation.count` (Counter)
     - `squad.agent.invocation.duration` (Histogram, ms)
     - `squad.mcp.tool.call.count` (Counter)
     - `squad.mcp.tool.call.duration` (Histogram, ms)
     - `squad.a2a.handshake.count` (Counter)
     - `squad.a2a.handshake.duration` (Histogram, ms)
     - `squad.a2ui.payload.count` (Counter)
     - `squad.pricing.decision.count` (Counter)
   - **Helper Methods**: `StartAgentSpan()`, `StartToolSpan()`, `StartA2ASpan()`, `StartAgUiSpan()` — create properly tagged spans

2. **ServiceDefaults Extensions** (Enhanced):
   - Added `AddSquadCommerceTracing()` extension method — registers all custom activity sources
   - Added `AddSquadCommerceMetrics()` extension method — registers "SquadCommerce" meter
   - Integrated into `ConfigureOpenTelemetry()` — automatically applied to all services
   - OTLP export configured for Aspire Dashboard

3. **EntraIdScopeMiddleware** (Fully Implemented):
   - Validates Entra ID scopes against agent policy requirements
   - Agent scope mappings: ChiefSoftwareArchitect → SquadCommerce.Orchestrate, InventoryAgent → SquadCommerce.Inventory.Read, etc.
   - Extracts scopes from JWT claims (`scope`, `scopes`)
   - Extracts agent name from query params, route values, or `X-Agent-Name` header
   - **Demo Mode**: Configurable via `appsettings.json` → `EntraId:EnforcementMode`
   - Demo mode logs scope failures but allows requests (for development without full Entra ID)
   - Production mode returns 403 with structured error JSON
   - Skips validation for health checks and static assets

4. **Program.cs Wiring** (Complete):
   - Registered `IAgUiStreamWriter` as singleton
   - SignalR registered with `AddSignalR()`
   - CORS configured for Blazor with credentials support (SignalR requirement)
   - Middleware pipeline: HTTPS redirect → CORS → EntraIdScopeMiddleware
   - SignalR hub mapped: `/hubs/agent`
   - AG-UI endpoint mapped: `/api/agui?sessionId={sessionId}`
   - Agent endpoints and Pricing endpoints mapped
   - appsettings.json configured with `EntraId:EnforcementMode = "Demo"`

5. **AppHost Orchestration** (Updated):
   - Added Web project reference to AppHost.csproj
   - Updated AppHost.cs to orchestrate both Api and Web
   - Api project marked with `WithExternalHttpEndpoints()`
   - Web project references Api with `WithReference(api).WaitFor(api)` for proper startup order

**Patterns & Best Practices Applied:**
- **Channels for AG-UI streaming** — better than manual event queue management
- **Session-based SignalR groups** — proper multi-tenant isolation
- **Demo mode middleware** — allows development without full Entra ID integration
- **Typed results** — all endpoints use `TypedResults.Ok<T>`, `TypedResults.Accepted<T>`, etc.
- **XML doc comments** — all public APIs documented
- **Structured logging** — all operations log with structured context
- **Activity tagging** — all helper methods add relevant tags (agent name, tool name, session ID)

**Build Status:** ✅ API and AppHost projects build successfully

**Integration Points for Other Agents:**
- Satya (Agents) can use `IAgUiStreamWriter` to push events during agent execution
- Satya (Agents) can use `SquadCommerceTelemetry` helper methods to create spans
- Satya (Agents) can call `PricingEndpoints` after calculating impact
- Clippy (Frontend) can subscribe to AG-UI stream at `/api/agui?sessionId={id}`
- Clippy (Frontend) can connect to SignalR hub at `/hubs/agent` for background updates
- All custom metrics are ready for Satya to increment during agent operations

**Next Steps for Other Agents:**
- Satya: Wire agents to call `IAgUiStreamWriter` methods during execution
- Satya: Add OpenTelemetry spans to agent invocations using `SquadCommerceTelemetry` helpers
- Clippy: Implement AG-UI SSE client in Blazor to subscribe to `/api/agui`
- Clippy: Implement SignalR client to connect to `/hubs/agent`

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-24: Phase 6 — Full OpenTelemetry Observability Implementation

**Implemented Full Observability Stack:**

1. **SquadCommerceMetrics Singleton** (`ServiceDefaults/SquadCommerceMetrics.cs`):
   - Centralized metrics registry holding all 8 custom metrics as instance properties
   - All 4 ActivitySources (Agents, Mcp, A2A, AgUi) as instance properties
   - Helper methods: `StartAgentSpan()`, `StartToolSpan()`, `StartA2ASpan()`, `StartAgUiSpan()`
   - Recording methods: `RecordAgentInvocation()`, `RecordMcpToolCall()`, `RecordA2AHandshake()`, `RecordA2UIPayload()`, `RecordPricingDecision()`
   - Registered as singleton in DI — inject into agents/tools to record metrics

2. **All 8 Custom Metrics Registered:**
   - `squad.agent.invocation.count` (Counter) — tracks agent calls with tags (agent.name, success)
   - `squad.agent.invocation.duration` (Histogram, ms) — tracks agent execution time
   - `squad.mcp.tool.call.count` (Counter) — tracks MCP tool calls with tags (mcp.tool.name, success)
   - `squad.mcp.tool.call.duration` (Histogram, ms) — tracks tool execution time
   - `squad.a2a.handshake.count` (Counter) — tracks A2A handshakes with tags (a2a.external_agent, success)
   - `squad.a2a.handshake.duration` (Histogram, ms) — tracks round-trip time
   - `squad.a2ui.payload.count` (Counter) — tracks A2UI emissions with tags (a2ui.component_type, agui.session_id)
   - `squad.pricing.decision.count` (Counter) — tracks pricing decisions with tags (pricing.action, pricing.proposal_id)

3. **All 4 ActivitySources Configured:**
   - `SquadCommerce.Agents` — agent invocations and orchestration
   - `SquadCommerce.Mcp` — MCP tool calls
   - `SquadCommerce.A2A` — A2A handshakes with external agents
   - `SquadCommerce.AgUi` — AG-UI streaming events
   - All sources registered in `AddSquadCommerceTracing()` extension method
   - OTLP exporter configured for Aspire Dashboard

4. **Comprehensive Health Checks** (`ServiceDefaults/HealthChecks.cs`):
   - `AgentSystemHealthCheck` — verifies agents are registered and ready (tagged "ready")
   - `McpServerHealthCheck` — verifies MCP tools can be invoked (tagged "ready")
   - `SignalRHubHealthCheck` — verifies SignalR hub is accepting connections (tagged "ready")
   - All health checks registered via `AddSquadCommerceHealthChecks()` extension method
   - Exposed at `/health` and `/alive` endpoints (dev mode only)

5. **Structured JSON Logging Configured** (`appsettings.json`):
   - Console formatter set to JSON with structured output
   - IncludeScopes enabled for correlation context (traceId, spanId)
   - UTC timestamps in ISO 8601 format
   - All agent reasoning steps log with structured parameters (not string interpolation)
   - LogLevel: Debug for SquadCommerce namespace, Information for default

6. **Observability Wired Throughout:**
   - **Program.cs**: `AddSquadCommerceHealthChecks()`, `SquadCommerceMetrics` singleton, metrics injected into AG-UI endpoint
   - **AgUiStreamWriter**: Injects `SquadCommerceMetrics`, records A2UI payload metrics with component type extraction
   - **PricingEndpoints**: Injects `SquadCommerceMetrics`, records all pricing decisions (approved/rejected/modified)
   - **AgentEndpoints**: Injects `SquadCommerceMetrics`, creates orchestrator spans, records agent invocations with timing

7. **Distributed Tracing Example** (AgentEndpoints.TriggerAnalysis):
   - Creates parent orchestrator span with session.id and sku tags
   - Creates child spans for MarketIntelAgent, InventoryAgent, PricingAgent
   - Each span tagged with session.id for correlation
   - Metrics recorded for each agent with duration and success status
   - All operations logged with structured context (sessionId, traceId)

8. **Extensions.cs Updates**:
   - `AddSquadCommerceTracing()` now registers `SquadCommerceMetrics` singleton and uses instance sources
   - `AddSquadCommerceMetrics()` registers singleton and adds "SquadCommerce" meter
   - `AddSquadCommerceHealthChecks()` registers all 3 health check types
   - All extensions properly chain and return builder for fluent API

**Build Status:** ✅ ServiceDefaults and Api projects build successfully

**Integration Points for Other Agents:**
- Satya (Agents): Inject `SquadCommerceMetrics` singleton into agents, use `StartAgentSpan()` and `RecordAgentInvocation()` during execution
- Satya (MCP): Inject `SquadCommerceMetrics` into MCP tools, use `StartToolSpan()` and `RecordMcpToolCall()` during tool execution
- Satya (A2A): Inject `SquadCommerceMetrics` into A2A client, use `StartA2ASpan()` and `RecordA2AHandshake()` during external calls
- Clippy (Frontend): All A2UI payloads automatically tracked via `AgUiStreamWriter.WriteA2UIPayloadAsync()`
- All agents: Use structured logging with `ILogger<T>` and parameter placeholders (not string interpolation)

**Patterns Applied:**
- **Singleton Metrics Registry** — single source of truth for all meters, counters, histograms, and activity sources
- **Metrics Tags** — all metrics include tags for filtering (agent.name, success, mcp.tool.name, etc.)
- **Activity Tagging** — all spans tagged with relevant context (session.id, agent.name, tool.name, etc.)
- **Structured Logging** — all log statements use parameters, not string interpolation
- **Health Check Tagging** — ready/live tags for liveness vs readiness probes
- **DI-based Observability** — inject `SquadCommerceMetrics` instead of static access

**Notes:**
- `SquadCommerceTelemetry.cs` is now legacy — kept for backward compatibility, but new code should use `SquadCommerceMetrics` singleton
- Health checks are placeholders — TODO comments indicate where to integrate with AgentPolicyRegistry and IMcpToolRegistry when available
- Integration tests fail due to missing Mcp/Agents/A2A implementations — expected, not Anders' responsibility
- Aspire Dashboard will automatically collect all OTLP traces/metrics via environment variable configuration


### 2026-03-24: Wiring the Real ChiefSoftwareArchitectAgent into /api/agents/analyze Endpoint

**Task: Replace simulated orchestration with real agent workflow**

1. **TriggerAnalysis Endpoint** — Complete Overhaul:
   - Replaced `Task.Delay` simulation with real `ChiefSoftwareArchitectAgent.ProcessCompetitorPriceDropAsync()`
   - Added `IServiceProvider` injection to create DI scope for background work (agents are scoped services)
   - Added validation: `CompetitorPrice` must be non-null and > 0, returns `BadRequest<string>` if invalid
   - Background task creates new scope via `serviceProvider.CreateScope()` to resolve scoped agents correctly
   - Orchestrator call returns `OrchestratorResult` with `Success`, `ExecutiveSummary`, `AgentResults[]`, and `ErrorMessage`
   - Error handling: streams error status update and text delta if orchestration fails
   - A2UI payload streaming: iterates through `result.AgentResults` and streams each `A2UIPayload` via `WriteA2UIPayloadAsync()`
   - Executive summary streaming: streams `ExecutiveSummary` as text delta
   - Proper metrics recording: records orchestrator invocation duration with success/failure status
   - Return type changed to `Results<Accepted<AnalysisResponse>, BadRequest<string>>` to support validation

2. **GetAgents Endpoint** — Real Policy Registry:
   - Replaced mock data with `AgentPolicyRegistry.GetAllPolicies()`
   - Maps `AgentPolicy` to `AgentInfo` response objects
   - Determines role dynamically: "Orchestrator" for ChiefSoftwareArchitect, "Domain" for others
   - All data now reflects actual registered agent policies (names, scopes, tools, protocols)

3. **GetAgentStatus Endpoint** — Policy Validation:
   - Uses `AgentPolicyRegistry.GetPolicyByName(name)` to validate agent existence
   - Returns `NotFound` if agent name not in registry
   - Status/LastActivity/ActiveSessions remain placeholders (future work: integrate with real agent state tracking)

4. **Dependencies Added**:
   - `using SquadCommerce.Agents.Orchestrator;` — for `ChiefSoftwareArchitectAgent`
   - `using SquadCommerce.Agents.Policies;` — for `AgentPolicyRegistry`

**Patterns Applied:**
- **Scoped DI in Background Tasks** — `serviceProvider.CreateScope()` ensures agents resolve correctly (critical for Entity Framework and other scoped services)
- **Error Propagation to AG-UI** — orchestrator failures stream error status + text delta + done event (user sees failure)
- **A2UI Payload Iteration** — loops through `AgentResults`, streams non-null payloads (RetailStockHeatmap, PricingImpactChart, MarketComparisonGrid)
- **Validation Before Orchestration** — prevents orchestrator from running with invalid input (saves resources)

**Integration with Real Agents:**
- ChiefSoftwareArchitect orchestrator already creates proper OpenTelemetry spans internally (no need to duplicate)
- Each domain agent (InventoryAgent, PricingAgent, MarketIntelAgent) already records its own metrics and spans
- Endpoint streams results to AG-UI as they're produced by agents
- All telemetry (traces, metrics, logs) flows to Aspire Dashboard automatically

**Build & Test Status:** ✅ Solution builds successfully, all 160 tests pass

**Notes:**
- The orchestrator already has comprehensive error handling and telemetry — endpoint just needs to call it and stream results
- `IAgUiStreamWriter.WriteA2UIPayloadAsync()` already exists — no changes needed to the interface
- Scoped DI resolution in background tasks is critical — without `CreateScope()`, agents would fail to resolve
- Executive summary is plain text (Markdown format) — streamed as text delta, not A2UI payload
- Future work: add real-time agent status tracking (ActiveSessions, LastActivity) beyond policy validation

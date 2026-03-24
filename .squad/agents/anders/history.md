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


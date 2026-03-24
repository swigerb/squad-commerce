# Squad-Commerce Architecture

> **Version:** 1.0 · **Author:** Bill Gates (Lead) · **Date:** 2026-03-24
> **Status:** Approved — this is the canonical architecture reference for the project.

---

## 1. System Overview

**Squad-Commerce** is a Tier-1 Consumer Goods retail supply chain application that demonstrates how Microsoft Agent Framework (MAF), ASP.NET Core, SignalR, and Blazor work together with the A2A, MCP, AG-UI, and A2UI protocols.

### Business Problem

A regional retail chain needs to respond to competitor price changes in near-real-time. When a competitor drops prices on key SKUs, the system must:

1. Verify the competitor claim against external data sources
2. Analyze local inventory levels across stores
3. Calculate margin impact of matching or beating the price
4. Present a structured, actionable proposal to a store manager for approval or rejection

This is **not** a chatbot. It's a multi-agent supply chain decision system with a native UI.

### Key Principle

Every piece of data shown to the user comes from a verifiable tool call (MCP) or validated external source (A2A). **No hallucinated data.** Agents reason; tools provide facts.

---

## 2. Agent Architecture

### 2.1 Orchestrator

| Agent | Class | Role | Entra ID Scope |
|-------|-------|------|----------------|
| **ChiefSoftwareArchitect** | `ChiefSoftwareArchitectAgent` | Orchestrator — receives requests, decomposes work, delegates to domain agents, synthesizes final response | `SquadCommerce.Orchestrate` |

The orchestrator uses **MAF Graph-based Workflow** to coordinate all multi-step reasoning. It never calls MCP tools directly — it delegates to domain agents.

### 2.2 Domain Agents

| Agent | Class | Role | Allowed Tools | Protocol | Entra ID Scope |
|-------|-------|------|---------------|----------|----------------|
| **InventoryAgent** | `InventoryAgent` | Queries store inventory levels via MCP | `GetInventoryLevels` | MCP | `SquadCommerce.Inventory.Read` |
| **PricingAgent** | `PricingAgent` | Calculates margin impact, proposes/applies price changes | `GetInventoryLevels`, `UpdateStorePricing` | MCP | `SquadCommerce.Pricing.ReadWrite` |
| **MarketIntelAgent** | `MarketIntelAgent` | Retrieves and validates competitor pricing from external vendor agents | *(external A2A calls)* | A2A | `SquadCommerce.MarketIntel.Read` |

### 2.3 Agent Communication Patterns

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Frontend (A2UI)                     │
│  RetailStockHeatmap · PricingImpactChart · MarketComparison  │
└──────────────────────────┬──────────────────────────────────┘
                           │ AG-UI (SSE stream) + SignalR (state)
                           ▼
┌──────────────────────────────────────────────────────────────┐
│                   ASP.NET Core API Host                        │
│  MapAGUI endpoint · SignalR Hub · Entra ID middleware          │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────────┐
│              ChiefSoftwareArchitect (Orchestrator)             │
│              MAF Graph-based Workflow Engine                    │
│                                                                │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│   │ InventoryAgent│  │ PricingAgent │  │ MarketIntelAgent │   │
│   │   (MCP)       │  │   (MCP)      │  │     (A2A)        │   │
│   └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘   │
└──────────┼─────────────────┼───────────────────┼──────────────┘
           │                 │                   │
           ▼                 ▼                   ▼
    ┌─────────────┐   ┌─────────────┐   ┌───────────────┐
    │  MCP Server  │   │  MCP Server  │   │ External Vendor│
    │  (ERP/SQL)   │   │  (ERP/SQL)   │   │ Agent (A2A)    │
    └─────────────┘   └─────────────┘   └───────────────┘
```

### 2.4 AgentPolicy Enforcement

Every agent is registered with an `AgentPolicy` that the runtime enforces. No agent can exceed its policy.

```csharp
public sealed record AgentPolicy
{
    public required string AgentName { get; init; }
    public required bool EnforceA2UI { get; init; }
    public required bool RequireTelemetryTrace { get; init; }
    public required string PreferredProtocol { get; init; }
    public required IReadOnlyList<string> AllowedTools { get; init; }
    public required string EntraIdScope { get; init; }
}
```

**Policy table:**

| Agent | EnforceA2UI | RequireTelemetryTrace | PreferredProtocol | AllowedTools | EntraIdScope |
|-------|-------------|----------------------|-------------------|--------------|--------------|
| ChiefSoftwareArchitect | `true` | `true` | `AGUI` | `[]` (delegates only) | `SquadCommerce.Orchestrate` |
| InventoryAgent | `true` | `true` | `MCP` | `["GetInventoryLevels"]` | `SquadCommerce.Inventory.Read` |
| PricingAgent | `true` | `true` | `MCP` | `["GetInventoryLevels", "UpdateStorePricing"]` | `SquadCommerce.Pricing.ReadWrite` |
| MarketIntelAgent | `true` | `true` | `A2A` | `[]` (uses A2A, not MCP tools) | `SquadCommerce.MarketIntel.Read` |

---

## 3. Protocol Stack

### 3.1 MCP (Model Context Protocol)

MCP provides the data layer. Agents call MCP tools to read/write ERP and SQL data. **No agent fabricates data.**

**MCP Tools:**

| Tool | Description | Parameters | Returns |
|------|-------------|------------|---------|
| `GetInventoryLevels` | Query current stock levels by store and SKU | `storeIds: string[]`, `skuIds: string[]` | `InventorySnapshot[]` — store, SKU, quantity, lastUpdated |
| `UpdateStorePricing` | Apply approved price changes to store POS system | `storeId: string`, `priceChanges: PriceChange[]` | `PricingUpdateResult` — success/failure per SKU |

**Error handling:** MCP tool failures are escalated to the orchestrator with structured error payloads. Agents never retry silently — the orchestrator decides retry strategy.

### 3.2 A2A (Agent-to-Agent)

A2A is the protocol for communicating with **external** vendor agents (e.g., a market data provider). The handshake protocol:

1. **Discovery** — ChiefSoftwareArchitect publishes an Agent Card; external agents discover it
2. **Task Request** — MarketIntelAgent sends a structured task to the external vendor agent
3. **Task Response** — External agent returns competitor pricing data
4. **Validation** — MarketIntelAgent validates external data against internal telemetry before trusting it

**Key constraint:** External data from A2A is **never shown raw** to the user. It's always validated against internal inventory/pricing data first.

### 3.3 AG-UI (Agent-to-UI)

AG-UI is the streaming protocol between the backend and the Blazor frontend.

- **Endpoint:** `app.MapAGUI("/api/agui")` — SSE (Server-Sent Events) stream
- **Events:** Text deltas, tool call notifications, state updates, A2UI payloads
- **Status updates:** Real-time transparency messages (e.g., "InventoryAgent is querying MCP inventory server...")

### 3.4 A2UI (Agent-to-UI Components)

A2UI is the structured JSON payload format for rendering native Blazor components instead of raw text. **No raw markdown tables for complex data.**

When an agent produces structured data, it emits an A2UI payload:

```json
{
  "type": "a2ui",
  "renderAs": "RetailStockHeatmap",
  "data": { ... }
}
```

The Blazor frontend intercepts these payloads and renders the appropriate native component.

### 3.5 SignalR Sidecar

SignalR provides a parallel channel for **background state updates** that don't fit the AG-UI request/response stream:

- Agent lifecycle events (started, completed, failed)
- Long-running operation progress
- Push notifications for new competitor alerts
- Session state synchronization across tabs

**Hub:** `AgentHub` at `/hubs/agent`

---

## 4. A2UI Components

### 4.1 RetailStockHeatmap

**Renders when:** InventoryAgent returns inventory snapshot data
**Visualization:** Grid — rows are stores, columns are SKUs, cells are color-coded by stock level (red < 10%, yellow < 30%, green ≥ 30% of target)
**A2UI Payload:**

```json
{
  "type": "a2ui",
  "renderAs": "RetailStockHeatmap",
  "data": {
    "stores": [
      {
        "storeId": "STORE-001",
        "storeName": "Downtown Flagship",
        "inventory": [
          { "skuId": "SKU-100", "skuName": "Widget Pro", "quantity": 45, "target": 200, "level": "yellow" }
        ]
      }
    ],
    "generatedAt": "2026-03-24T12:00:00Z"
  }
}
```

### 4.2 PricingImpactChart

**Renders when:** PricingAgent calculates margin impact of proposed price changes
**Visualization:** Waterfall/bar chart — current price → proposed price → margin delta per SKU
**A2UI Payload:**

```json
{
  "type": "a2ui",
  "renderAs": "PricingImpactChart",
  "data": {
    "proposals": [
      {
        "skuId": "SKU-100",
        "skuName": "Widget Pro",
        "currentPrice": 29.99,
        "proposedPrice": 24.99,
        "competitorPrice": 22.99,
        "currentMargin": 0.35,
        "proposedMargin": 0.18,
        "revenueImpact": -12500.00,
        "volumeUpliftEstimate": 0.15
      }
    ],
    "totalRevenueImpact": -37500.00,
    "generatedAt": "2026-03-24T12:00:00Z"
  }
}
```

### 4.3 MarketComparisonGrid

**Renders when:** MarketIntelAgent returns validated competitor data
**Visualization:** Sortable table — our price vs. competitor price, delta %, trend indicator, source attribution
**A2UI Payload:**

```json
{
  "type": "a2ui",
  "renderAs": "MarketComparisonGrid",
  "data": {
    "competitors": [
      {
        "competitorName": "MegaMart",
        "items": [
          {
            "skuId": "SKU-100",
            "skuName": "Widget Pro",
            "ourPrice": 29.99,
            "theirPrice": 22.99,
            "deltaPercent": -23.3,
            "trend": "dropping",
            "verifiedViaA2A": true,
            "sourceAgent": "MegaMart-PricingAgent"
          }
        ]
      }
    ],
    "generatedAt": "2026-03-24T12:00:00Z"
  }
}
```

---

## 5. Core Workflow: Competitor Price Drop Scenario

This is the primary end-to-end scenario. Every protocol and component gets exercised.

```
Trigger: Store manager receives competitor price alert (or manual check)
         ↓
Step 1:  Request arrives at AG-UI endpoint (/api/agui)
         → ChiefSoftwareArchitect receives the task
         → Status update via SignalR: "Analyzing competitor price change..."
         ↓
Step 2:  ChiefSoftwareArchitect delegates to MarketIntelAgent
         → MarketIntelAgent performs A2A handshake with external vendor agent
         → Retrieves competitor pricing for affected SKUs
         → Validates data against internal records
         → Returns validated competitor data
         → AG-UI streams status: "MarketIntelAgent verified competitor pricing via A2A"
         ↓
Step 3:  ChiefSoftwareArchitect delegates to InventoryAgent
         → InventoryAgent calls MCP tool: GetInventoryLevels(stores, skus)
         → Returns current inventory snapshot for affected stores/SKUs
         → Emits A2UI payload: RetailStockHeatmap
         → AG-UI streams status: "InventoryAgent queried inventory for 12 stores"
         ↓
Step 4:  ChiefSoftwareArchitect delegates to PricingAgent
         → PricingAgent receives inventory data + competitor data
         → Calls MCP tool: GetInventoryLevels (for cost basis)
         → Calculates margin impact, revenue delta, volume uplift estimate
         → Emits A2UI payloads: PricingImpactChart + MarketComparisonGrid
         → AG-UI streams status: "PricingAgent calculated impact across 5 SKUs"
         ↓
Step 5:  ChiefSoftwareArchitect synthesizes results
         → Produces executive summary text (streamed via AG-UI)
         → All A2UI components render in Blazor UI simultaneously
         → Store manager sees: heatmap + impact chart + comparison grid + summary
         ↓
Step 6:  Store manager reviews and decides
         → Approve: PricingAgent calls MCP tool UpdateStorePricing
         → Reject: Logged as declined, no action taken
         → Modify: Manager adjusts proposed prices, re-triggers Step 4
         ↓
Step 7:  Outcome logged
         → OpenTelemetry trace completed end-to-end
         → Decision audit trail stored
         → SignalR pushes confirmation: "Pricing updated for 3 stores"
```

**All steps traced.** Every agent invocation, tool call, and A2A handshake produces an OpenTelemetry span.

---

## 6. Project Structure

```
SquadCommerce.sln
│
├── src/
│   ├── SquadCommerce.AppHost/                    # .NET Aspire orchestrator
│   │   └── Program.cs                            # Wires all projects, dashboard config
│   │
│   ├── SquadCommerce.ServiceDefaults/            # Aspire shared configuration
│   │   └── Extensions.cs                         # OpenTelemetry, health checks, resilience
│   │
│   ├── SquadCommerce.Api/                        # ASP.NET Core Web API host
│   │   ├── Program.cs                            # DI, middleware, MapAGUI, SignalR hub
│   │   ├── Hubs/
│   │   │   └── AgentHub.cs                       # SignalR hub for background state
│   │   ├── Endpoints/
│   │   │   ├── AgentEndpoints.cs                 # REST endpoints for agent management
│   │   │   └── PricingEndpoints.cs               # Approval/rejection endpoints
│   │   └── Middleware/
│   │       └── EntraIdScopeMiddleware.cs          # Validates Entra ID scopes per request
│   │
│   ├── SquadCommerce.Agents/                     # MAF agent implementations
│   │   ├── Orchestrator/
│   │   │   ├── ChiefSoftwareArchitectAgent.cs    # Orchestrator agent
│   │   │   └── RetailWorkflow.cs                 # MAF graph-based workflow definition
│   │   ├── Domain/
│   │   │   ├── InventoryAgent.cs                 # Inventory domain agent
│   │   │   ├── PricingAgent.cs                   # Pricing domain agent
│   │   │   └── MarketIntelAgent.cs               # Market intelligence agent
│   │   ├── Policies/
│   │   │   ├── AgentPolicy.cs                    # Policy record definition
│   │   │   ├── AgentPolicyRegistry.cs            # Registers policies per agent
│   │   │   └── PolicyEnforcementFilter.cs        # MAF filter that enforces policies
│   │   └── Registration/
│   │       └── AgentServiceExtensions.cs         # AddSquadCommerceAgents() DI extension
│   │
│   ├── SquadCommerce.Mcp/                        # MCP server and tool implementations
│   │   ├── McpServerSetup.cs                     # MCP server registration
│   │   ├── Tools/
│   │   │   ├── GetInventoryLevelsTool.cs         # MCP tool: inventory query
│   │   │   └── UpdateStorePricingTool.cs         # MCP tool: pricing update
│   │   └── Data/
│   │       ├── InventoryRepository.cs            # ERP/SQL data access
│   │       └── PricingRepository.cs              # Pricing data access
│   │
│   ├── SquadCommerce.A2A/                        # A2A protocol implementation
│   │   ├── AgentCard.cs                          # Agent Card definition
│   │   ├── A2AClient.cs                          # Client for calling external agents
│   │   ├── A2AServer.cs                          # Server for receiving external requests
│   │   └── Validation/
│   │       └── ExternalDataValidator.cs          # Validates A2A responses against internal data
│   │
│   ├── SquadCommerce.Contracts/                  # Shared DTOs, interfaces, A2UI payloads
│   │   ├── A2UI/
│   │   │   ├── A2UIPayload.cs                    # Base A2UI payload type
│   │   │   ├── RetailStockHeatmapData.cs         # Heatmap data contract
│   │   │   ├── PricingImpactChartData.cs         # Impact chart data contract
│   │   │   └── MarketComparisonGridData.cs       # Comparison grid data contract
│   │   ├── Models/
│   │   │   ├── InventorySnapshot.cs              # Inventory data model
│   │   │   ├── PriceChange.cs                    # Price change request
│   │   │   ├── PricingUpdateResult.cs            # Pricing update response
│   │   │   └── CompetitorPricing.cs              # Competitor data model
│   │   └── Interfaces/
│   │       ├── IInventoryRepository.cs
│   │       ├── IPricingRepository.cs
│   │       └── IA2AClient.cs
│   │
│   └── SquadCommerce.Web/                        # Blazor frontend
│       ├── Program.cs                            # Blazor app configuration
│       ├── Components/
│       │   ├── A2UI/
│       │   │   ├── A2UIRenderer.razor             # Dispatches A2UI payloads to components
│       │   │   ├── RetailStockHeatmap.razor       # Heatmap component
│       │   │   ├── PricingImpactChart.razor       # Impact chart component
│       │   │   └── MarketComparisonGrid.razor     # Comparison grid component
│       │   ├── Chat/
│       │   │   ├── AgentChat.razor                # AG-UI streaming chat panel
│       │   │   └── AgentStatusBar.razor           # Real-time agent status
│       │   └── Layout/
│       │       └── MainLayout.razor               # App shell
│       ├── Services/
│       │   ├── AgUiStreamService.cs               # AG-UI SSE client
│       │   └── SignalRStateService.cs             # SignalR client for background state
│       └── wwwroot/
│
└── tests/
    ├── SquadCommerce.Agents.Tests/               # Agent unit tests
    ├── SquadCommerce.Mcp.Tests/                  # MCP tool tests
    ├── SquadCommerce.A2A.Tests/                  # A2A protocol tests
    └── SquadCommerce.Integration.Tests/          # End-to-end scenario tests
```

### Project Dependencies

```
AppHost → Api, Web (orchestrates both)
Api → Agents, Mcp, A2A, Contracts, ServiceDefaults
Agents → Contracts, Mcp (tool interfaces), A2A (client interface)
Mcp → Contracts
A2A → Contracts
Web → Contracts, ServiceDefaults
ServiceDefaults → (standalone)
Contracts → (standalone, no dependencies)
```

---

## 7. Technology Stack

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| Runtime | .NET | 10 | Latest LTS runtime |
| Web Framework | ASP.NET Core | 10 | API host, middleware, endpoints |
| Agent Framework | Microsoft Agent Framework (MAF) | Latest | Agent orchestration, graph workflows |
| Real-time | SignalR | (ASP.NET Core built-in) | Background state push to UI |
| Frontend | Blazor | 10 (Server + WASM hybrid) | Native UI with A2UI components |
| Orchestration | .NET Aspire | Latest | Local dev orchestration, dashboard |
| Observability | OpenTelemetry | .NET SDK | Distributed tracing, metrics, logs |
| Identity | Microsoft Entra ID | MSAL | Scope-based agent authorization |
| Data | Entity Framework Core + SQL | 10 | ERP/inventory data access |
| Protocols | MCP, A2A, AG-UI | Latest specs | Agent communication |

### Key NuGet Packages (Expected)

- `Microsoft.Extensions.AI` — MAF core
- `Microsoft.Extensions.AI.Agents` — MAF agent abstractions
- `Microsoft.AspNetCore.SignalR` — Real-time communication
- `OpenTelemetry.Extensions.Hosting` — OTel integration
- `OpenTelemetry.Exporter.OtlpProtocol` — OTLP export to Aspire
- `Microsoft.Identity.Web` — Entra ID integration
- `Aspire.Hosting` — Aspire AppHost

---

## 8. Observability

### 8.1 OpenTelemetry Strategy

**Every action is traced.** The tracing hierarchy:

```
Trace: CompetitorPriceDropScenario
├── Span: ChiefSoftwareArchitect.Orchestrate
│   ├── Span: MarketIntelAgent.Execute
│   │   ├── Span: A2A.Handshake (external vendor)
│   │   └── Span: A2A.ValidateResponse
│   ├── Span: InventoryAgent.Execute
│   │   └── Span: MCP.GetInventoryLevels
│   ├── Span: PricingAgent.Execute
│   │   ├── Span: MCP.GetInventoryLevels
│   │   └── Span: PricingAgent.CalculateImpact
│   └── Span: ChiefSoftwareArchitect.Synthesize
├── Span: AGUI.StreamResponse
└── Span: SignalR.PushStateUpdate
```

### 8.2 Custom Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `squad.agent.invocation.count` | Counter | Number of agent invocations by agent name |
| `squad.agent.invocation.duration` | Histogram | Agent execution time in ms |
| `squad.mcp.tool.call.count` | Counter | MCP tool calls by tool name |
| `squad.mcp.tool.call.duration` | Histogram | MCP tool execution time in ms |
| `squad.a2a.handshake.count` | Counter | A2A handshakes by external agent |
| `squad.a2a.handshake.duration` | Histogram | A2A round-trip time in ms |
| `squad.a2ui.payload.count` | Counter | A2UI payloads emitted by component type |
| `squad.pricing.decision.count` | Counter | Pricing decisions (approved/rejected/modified) |

### 8.3 Aspire Dashboard

The Aspire AppHost configures the dashboard to show:
- All agent traces with full span hierarchy
- MCP tool call performance
- A2A handshake latency
- SignalR connection health
- Structured JSON logs from all agent reasoning steps

### 8.4 Structured Logging

All agent reasoning steps emit structured JSON logs:

```json
{
  "timestamp": "2026-03-24T12:00:00Z",
  "level": "Information",
  "agent": "PricingAgent",
  "action": "CalculateImpact",
  "traceId": "abc123",
  "spanId": "def456",
  "data": {
    "skuCount": 5,
    "totalRevenueImpact": -37500.00,
    "recommendation": "partial_match"
  }
}
```

---

## 9. Security: Entra ID

### 9.1 Scope Model

Each agent operates under a specific Entra ID scope. The `EntraIdScopeMiddleware` validates that the current authentication context includes the required scope before any agent executes.

| Scope | Grants Access To | Agents Using |
|-------|-----------------|--------------|
| `SquadCommerce.Orchestrate` | Invoke orchestrator, delegate to domain agents | ChiefSoftwareArchitect |
| `SquadCommerce.Inventory.Read` | Read inventory levels (MCP GetInventoryLevels) | InventoryAgent |
| `SquadCommerce.Pricing.ReadWrite` | Read inventory + write pricing (MCP UpdateStorePricing) | PricingAgent |
| `SquadCommerce.MarketIntel.Read` | Query external market data (A2A) | MarketIntelAgent |

### 9.2 Enforcement Flow

```
Request → Entra ID JWT validation → Extract scopes from token claims
→ AgentPolicyRegistry.GetPolicy(agentName)
→ Verify token contains required scope
→ Allow/Deny agent execution
```

### 9.3 App Registration

A single Entra ID app registration (`SquadCommerce`) with:
- API permissions exposing the four scopes above
- Client credential flow for agent-to-agent calls
- User delegation flow for store manager interactions

---

## 10. Key Interfaces

### 10.1 Agent Contracts

```csharp
// Base interface for all domain agents
public interface IDomainAgent
{
    string Name { get; }
    AgentPolicy Policy { get; }
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct);
}

// Agent result can contain text, A2UI payloads, or both
public sealed record AgentResult
{
    public string? TextResponse { get; init; }
    public IReadOnlyList<A2UIPayload> UIPayloads { get; init; } = [];
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

// A2UI payload base
public sealed record A2UIPayload
{
    public required string Type { get; init; } // always "a2ui"
    public required string RenderAs { get; init; } // component name
    public required object Data { get; init; } // typed data payload
}
```

### 10.2 MCP Tool Contracts

```csharp
public interface IInventoryRepository
{
    Task<IReadOnlyList<InventorySnapshot>> GetLevelsAsync(
        string[] storeIds, string[] skuIds, CancellationToken ct);
}

public interface IPricingRepository
{
    Task<PricingUpdateResult> UpdatePricingAsync(
        string storeId, IReadOnlyList<PriceChange> changes, CancellationToken ct);
}
```

### 10.3 A2A Client Contract

```csharp
public interface IA2AClient
{
    Task<A2ATaskResponse> SendTaskAsync(
        string externalAgentUrl, A2ATaskRequest request, CancellationToken ct);
}
```

---

## 11. Work Breakdown — Phased Delivery

### Phase 1: Solution Scaffolding + MAF Setup + Basic Agent Wiring

**Goal:** Buildable solution with all projects, DI wired, agents registered but stubbed.

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 1.1 | Create `SquadCommerce.sln` with all projects | Anders | Solution file, .csproj files, project references |
| 1.2 | Configure Aspire AppHost + ServiceDefaults | Anders | `AppHost/Program.cs`, `ServiceDefaults/Extensions.cs` |
| 1.3 | Define `Contracts` — all DTOs, interfaces, A2UI payloads | Satya | Contracts project fully populated |
| 1.4 | Implement `AgentPolicy` + `AgentPolicyRegistry` | Satya | Policy enforcement foundation |
| 1.5 | Stub all four agents with MAF registration | Satya | `AddSquadCommerceAgents()` extension, agents return hardcoded data |
| 1.6 | Wire `Api/Program.cs` — DI, basic health endpoint | Anders | API project boots and responds |
| 1.7 | Create Blazor `Web` project with shell layout | Clippy | Blazor app boots with MainLayout |
| 1.8 | Unit tests for AgentPolicy enforcement | Steve | Policy tests pass |

### Phase 2: MCP Server + Inventory/Pricing Tools

**Goal:** MCP tools return real data from an in-memory or SQLite store. Agents call them.

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 2.1 | Implement MCP server registration in `SquadCommerce.Mcp` | Satya | MCP server mounts tools |
| 2.2 | Implement `GetInventoryLevelsTool` with repository | Satya | Tool returns inventory from data store |
| 2.3 | Implement `UpdateStorePricingTool` with repository | Satya | Tool updates pricing in data store |
| 2.4 | Seed sample inventory/pricing data | Anders | Realistic demo data for 5 stores, 20 SKUs |
| 2.5 | Wire `InventoryAgent` to call `GetInventoryLevels` via MCP | Satya | Agent produces real InventorySnapshot data |
| 2.6 | Wire `PricingAgent` to call MCP tools + calculate margins | Satya | Agent produces PricingImpactChart data |
| 2.7 | Unit tests for MCP tools | Steve | Tool tests with in-memory data |
| 2.8 | Unit tests for InventoryAgent and PricingAgent | Steve | Agent tests with mocked MCP |

### Phase 3: A2A Protocol + Market Intel Integration

**Goal:** MarketIntelAgent communicates with a mock external vendor agent via A2A.

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 3.1 | Implement `A2AClient` and `A2AServer` in `SquadCommerce.A2A` | Satya | A2A handshake protocol working |
| 3.2 | Implement `AgentCard` publishing | Satya | Agent discovery endpoint |
| 3.3 | Create mock external vendor agent for testing | Satya | Returns realistic competitor pricing |
| 3.4 | Implement `ExternalDataValidator` | Satya | Validates A2A responses against internal data |
| 3.5 | Wire `MarketIntelAgent` end-to-end | Satya | Agent queries external, validates, returns comparison data |
| 3.6 | Implement `RetailWorkflow` graph (orchestrator flow) | Satya | ChiefSoftwareArchitect delegates correctly |
| 3.7 | A2A protocol tests | Steve | Handshake, validation, error handling |
| 3.8 | Integration test: full orchestrator flow | Steve | All three agents called in correct order |

### Phase 4: AG-UI Streaming + A2UI Components in Blazor

**Goal:** Store manager sees the full competitor-price-drop experience with native UI components.

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 4.1 | Configure `MapAGUI` endpoint in API | Anders | AG-UI SSE streaming works |
| 4.2 | Implement `AgentHub` SignalR hub | Anders | Background state updates flowing |
| 4.3 | Implement `AgUiStreamService` in Blazor | Clippy | Blazor receives AG-UI events |
| 4.4 | Implement `SignalRStateService` in Blazor | Clippy | Blazor receives SignalR state |
| 4.5 | Build `A2UIRenderer` dispatcher component | Clippy | Routes A2UI payloads to correct component |
| 4.6 | Build `RetailStockHeatmap` Blazor component | Clippy | Renders inventory heatmap |
| 4.7 | Build `PricingImpactChart` Blazor component | Clippy | Renders pricing waterfall chart |
| 4.8 | Build `MarketComparisonGrid` Blazor component | Clippy | Renders sortable comparison table |
| 4.9 | Build `AgentChat` + `AgentStatusBar` | Clippy | Chat panel with streaming + status |
| 4.10 | Build approval/rejection UX flow | Clippy | Manager can approve, reject, or modify |
| 4.11 | UI component tests | Steve | Blazor component tests for all A2UI |

### Phase 5: OpenTelemetry + Aspire Dashboard + Entra ID

**Goal:** Full observability and security. Every action traced, every agent authorized.

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 5.1 | Configure OpenTelemetry in ServiceDefaults | Anders | Tracing, metrics, logging configured |
| 5.2 | Add spans to all agent invocations | Satya | Agent spans with correct hierarchy |
| 5.3 | Add spans to MCP tool calls | Satya | Tool call spans with parameters |
| 5.4 | Add spans to A2A handshakes | Satya | A2A spans with external agent info |
| 5.5 | Register custom metrics | Anders | All 8 custom metrics emitting |
| 5.6 | Configure Aspire Dashboard to display traces | Anders | Dashboard shows full trace hierarchy |
| 5.7 | Implement `EntraIdScopeMiddleware` | Anders | Scope validation on every request |
| 5.8 | Configure Entra ID app registration | Anders | Scopes defined, MSAL integrated |
| 5.9 | Wire AgentPolicy scope enforcement | Satya | Agents cannot exceed their scopes |
| 5.10 | Security tests — scope enforcement | Steve | Unauthorized access denied |
| 5.11 | Observability tests — trace completeness | Steve | All spans present in trace |

### Phase 6: End-to-End Scenario Testing

**Goal:** The full competitor-price-drop scenario works flawlessly. Demo-ready.

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 6.1 | E2E test: competitor price drop → approval | Steve | Full scenario passes |
| 6.2 | E2E test: competitor price drop → rejection | Steve | Rejection flow works |
| 6.3 | E2E test: competitor price drop → modify | Steve | Modification re-triggers pricing |
| 6.4 | E2E test: MCP tool failure → graceful degradation | Steve | Error handling works |
| 6.5 | E2E test: A2A handshake failure → fallback | Steve | External agent unavailable handled |
| 6.6 | Performance test: 10 concurrent scenarios | Steve | No resource leaks, acceptable latency |
| 6.7 | Architecture review + polish | Bill | Final review of all code |
| 6.8 | Demo walkthrough documentation | All | README with demo steps |

---

## Appendix A: Design Decisions

| ID | Decision | Rationale |
|----|----------|-----------|
| D1 | Separate `Agents`, `Mcp`, `A2A` projects (not all-in-one API) | Each protocol concern is independently testable and replaceable. Showcase clarity over deployment convenience. |
| D2 | `Contracts` project has zero dependencies | Prevents circular references. Every other project can reference it without pulling in implementation details. |
| D3 | AgentPolicy is a record, not a class | Immutable by design. Policies are defined at registration time and never mutated. |
| D4 | ChiefSoftwareArchitect never calls MCP tools directly | Clean separation: orchestrator orchestrates, domain agents execute. Prevents the orchestrator from becoming a god agent. |
| D5 | A2A data is always validated before reaching the UI | External data is untrusted. The `ExternalDataValidator` cross-references against internal inventory/pricing before surfacing. |
| D6 | SignalR is a sidecar, not the primary transport | AG-UI (SSE) handles request/response streaming. SignalR handles push-only background events. Two channels, two purposes. |
| D7 | Blazor Server + WASM hybrid | Server for initial load speed, WASM for rich client interactivity on A2UI components. |
| D8 | In-memory/SQLite for demo data, not a real ERP | This is a showcase. Real ERP integration is out of scope. The MCP abstraction means swapping in a real backend is trivial. |

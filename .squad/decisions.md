# Squad Decisions

## Active Decisions

### 2026-03-24T12:41:48Z: Project Orchestration Directive
**By:** Brian Swiger (via Copilot)
**What:** Comprehensive architectural and operational directive for Squad-Commerce

**1. Operational Protocol:**
- All multi-step reasoning via MAF Graph-based Workflow
- No agent acts in isolation without reporting state to @ChiefSoftwareArchitect
- MCP Server tools for ERP/SQL data (GetInventoryLevels, UpdateStorePricing) — no hallucinated data, escalate errors
- A2A Handshake protocol for external vendor agents — validate external data against internal telemetry

**2. Communication Standards:**
- AG-UI protocol via MapAGUI endpoint for all streaming responses
- Status updates for UI transparency (e.g., "@TheExplorer is querying the MCP inventory server...")
- A2UI JSON payloads REQUIRED for complex data (NO raw markdown tables):
  - Inventory levels → Render: RetailStockHeatmap
  - Price changes → Render: PricingImpactChart
  - Competitor comparisons → Render: MarketComparisonGrid

**3. Enterprise Engineering Constraints:**
- OpenTelemetry tracing on every action, tool call, and agent handoff
- Structured JSON logging for all reasoning steps (Aspire Dashboard auditability)
- SignalR sidecar for background state updates to Blazor UI
- Entra ID scope enforcement — agents cannot access tools outside their claims

**4. Mission Logic:**
- "If a competitor drops prices → analyze local inventory (MCP) → verify competitor claim (A2A) → calculate margin impact → present native UI proposal (A2UI) for store manager approval/rejection"

**5. Implementation Pattern:**
- AgentPolicy enforcement in C# with EnforceA2UI, RequireTelemetryTrace, PreferredProtocol, AllowedTools
- AddAgent<ChiefSoftwareArchitect> as the orchestrator agent

**Why:** User request — captured for team memory. This is the foundational architectural directive for the entire project.

---

### 2026-03-24: Architecture Plan — Bill Gates (Lead)
**By:** Bill Gates  
**What:** Foundational architecture decisions for Squad-Commerce (9 decisions)

**D1. Solution Structure:** Separate projects for `Agents`, `Mcp`, `A2A`, `Contracts`, `Api`, `Web`, `AppHost`, `ServiceDefaults`. Each protocol concern is independently testable. Contracts project has zero dependencies to prevent circular references.

**D2. Agent Naming:** Four MAF agents — `ChiefSoftwareArchitectAgent` (orchestrator), `InventoryAgent`, `PricingAgent`, `MarketIntelAgent`. The orchestrator never calls MCP tools directly — it delegates to domain agents only.

**D3. AgentPolicy Pattern:** Immutable `record AgentPolicy` with `EnforceA2UI`, `RequireTelemetryTrace`, `PreferredProtocol`, `AllowedTools`, `EntraIdScope`. Registered at startup, enforced by `PolicyEnforcementFilter` in the MAF pipeline.

**D4. Protocol Separation:** AG-UI (SSE) is the primary request/response streaming channel. SignalR is a sidecar for background-only push events. Two channels, two purposes — no conflation.

**D5. A2A Validation Rule:** External data from A2A is never shown raw to the user. `ExternalDataValidator` cross-references against internal data before surfacing.

**D6. A2UI Component Set:** Three native Blazor components — `RetailStockHeatmap`, `PricingImpactChart`, `MarketComparisonGrid`. No raw markdown for complex data. All typed via `A2UIPayload` record with `RenderAs` discriminator.

**D7. Entra ID Scopes:** Four scopes — `SquadCommerce.Orchestrate`, `SquadCommerce.Inventory.Read`, `SquadCommerce.Pricing.ReadWrite`, `SquadCommerce.MarketIntel.Read`. Enforced by middleware before agent execution.

**D8. Data Strategy:** In-memory or SQLite for demo data. MCP abstraction means swapping to real ERP is a repository implementation change, not an architecture change.

**D9. Phased Delivery:** Six phases — scaffolding → MCP → A2A → AG-UI/A2UI → observability/security → E2E testing. Each phase produces a working increment.

**Why:** These decisions establish the canonical architecture for Squad-Commerce. All team members should read `.squad/architecture.md` before starting any implementation work.

**Canonical reference:** `.squad/architecture.md`

---

### 2026-03-24: Test Architecture Decisions — Steve Ballmer (Tester)
**By:** Steve Ballmer  
**What:** 10 foundational test architecture decisions for Squad-Commerce

**T1. xUnit Standard:** Use xUnit exclusively for all test projects (unit, integration, E2E). Industry standard, built-in async/await support, parallel execution by default, clean syntax.

**T2. Integration Tests Over Mocks:** Prefer integration tests with real protocol implementations for A2A and MCP communication. Use mocks only for external dependencies outside our control. A2A and MCP protocols are complex — mocks can hide integration bugs.

**T3. Coverage Gates:** Minimum 80% code coverage across all projects. 100% coverage on critical paths: pricing calculation, A2A handshake, MCP invocation, Entra ID authorization, A2UI payload generation, SignalR broadcast, OpenTelemetry trace emission.

**T4. OpenTelemetry Validation:** All integration and E2E tests must validate OpenTelemetry trace emission. Use `TestTelemetryExporter` to capture spans in-memory and validate span names, attributes, and parent-child relationships.

**T5. Test Naming Convention:** All tests follow `Should_<ExpectedBehavior>_When_<Condition>` format. Example: `Should_CalculateCorrectMargin_When_CompetitorPriceDrops30Percent()`.

**T6. Test Project Structure:** One test project per source project with naming `<SourceProject>.<TestType>Tests`. Structure: `SquadCommerce.Agents.UnitTests`, `SquadCommerce.A2A.IntegrationTests`, `SquadCommerce.E2E.Tests`, etc.

**T7. In-Memory Databases:** Use EF Core InMemory provider for integration tests unless specific database behavior needs testing. Fast, isolated, deterministic.

**T8. bUnit for Blazor:** Use bUnit for Blazor component testing. Purpose-built for Blazor's rendering model, integrates with xUnit seamlessly.

**T9. Playwright for E2E:** Use Playwright for E2E UI automation (browser testing). Modern, fast, reliable. Supports Chromium, Firefox, WebKit. Built-in waiting mechanisms.

**T10. Test Review Checklist:** PR template includes test review checklist. Reviewers validate: all new APIs have tests, happy path + error handling tested, naming convention followed, no Thread.Sleep(), test data deterministic and isolated, telemetry spans validated, integration tests use TestServer.

**Why:** These decisions establish a world-class testing strategy demonstrating Microsoft excellence in AI development.

**Canonical reference:** `.squad/test-strategy.md`

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

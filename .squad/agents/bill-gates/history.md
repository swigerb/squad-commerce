# Project Context

- **Owner:** Brian Swiger
- **Project:** squad-commerce — A sample commerce application demonstrating Microsoft Agent Framework (MAF), A2A, MCP, AG-UI, and A2UI
- **Stack:** ASP.NET Core, SignalR, Blazor (A2UI), C#, Microsoft Agent Framework, MCP, A2A, AG-UI
- **Created:** 2026-03-24

## Core Context

Lead architect for squad-commerce. This project is a Microsoft showcase of excellence in AI development — demonstrating how MAF + ASP.NET Core + SignalR + Blazor work together with A2A (Agent-to-Agent), MCP (Model Context Protocol), and AG-UI (Agent-to-UI) protocols.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-24: Architecture Decomposition Complete
- **Canonical architecture doc:** `.squad/architecture.md` — all team members reference this
- **Decisions doc:** `.squad/decisions/inbox/bill-gates-architecture-plan.md` — 9 architectural decisions
- **Solution structure:** 8 src projects (`AppHost`, `ServiceDefaults`, `Api`, `Agents`, `Mcp`, `A2A`, `Contracts`, `Web`) + 4 test projects
- **Agent design:** 4 MAF agents — `ChiefSoftwareArchitectAgent` (orchestrator), `InventoryAgent`, `PricingAgent`, `MarketIntelAgent`
- **Key pattern:** `AgentPolicy` record enforces `EnforceA2UI`, `RequireTelemetryTrace`, `PreferredProtocol`, `AllowedTools`, `EntraIdScope` per agent
- **Protocol split:** AG-UI (SSE) for request/response streaming, SignalR sidecar for background push only
- **A2UI components:** `RetailStockHeatmap`, `PricingImpactChart`, `MarketComparisonGrid` — all typed via `A2UIPayload` with `RenderAs` discriminator
- **Contracts project has zero deps** — prevents circular references, every other project can reference it
- **Orchestrator never calls MCP directly** — delegates to domain agents only, preventing god-agent anti-pattern
- **6-phase delivery plan:** scaffolding → MCP → A2A → AG-UI/A2UI → observability/security → E2E testing
- **User preference:** Brian wants a showcase — code that teaches, not just functions. Architecture clarity over deployment convenience.

### 2026-03-24: Phase 6 Architecture Review Complete
- **Review report:** `.squad/review-report.md` — comprehensive compliance audit against architecture doc
- **Score: 8.5/10** — Exceptional individual components, pending final integration wiring
- **39 architecture compliance items verified ✅** — all core patterns, policies, agents, components, metrics, and telemetry match the spec
- **Critical gap: API not wired to agents** — `Api\Program.cs` doesn't call `AddSquadCommerceAgents()` or `AddSquadCommerceMcp()`. `TriggerAnalysis` runs simulated workflow, not real orchestrator. Fix is ~10 lines.
- **Interface evolution:** `IDomainAgent`, `IA2AClient`, `IInventoryRepository`, `IPricingRepository` signatures evolved from architecture doc to simpler, more practical forms — this is fine, just update the doc
- **Duplicate telemetry concern:** Both `SquadCommerceTelemetry` (static class) and `SquadCommerceMetrics` (DI singleton) create overlapping ActivitySources — need consolidation
- **PricingAgent concrete cast:** Casts `IPricingRepository` to `Mcp.Data.PricingRepository` for `GetCost()` — violates interface abstraction, add method to interface
- **A2AServer stubs:** All handler methods return hardcoded stub messages — server-side A2A incomplete
- **README drift:** Says `SquadCommerce.Shared` (should be `Contracts`), lists SQL Server prerequisite (data is in-memory), code example doesn't match actual DI pattern
- **Key learning:** Individual project quality was excellent across the board. The team built strong foundations. The gap was always going to be integration — that's the hardest part and should be the explicit focus of any remaining work.

### 2026-03-24: Aspire Local Development Architecture Decision
- **Request:** Brian asked if squad-commerce can run locally without containerizing Aspire (referenced retail-intelligence-studio as model)
- **Finding:** ✅ **squad-commerce already runs locally without Docker** — AppHost uses `AddProject<>()` (not containers), Aspire Dashboard is embedded
- **Analysis:** Compared against reference project — both use same pattern (Aspire 13.1.0, `AddProject<>()`, local service discovery via env vars)
- **Only difference:** Health checks. Reference exposes them in all environments; squad-commerce gates them to Development. This is intentional security vs. observability tradeoff.
- **OTLP exporter:** Both use OpenTelemetry; squad-commerce auto-detects (gracefully no-ops if endpoint missing), reference uses explicit URI. Functionally equivalent for local dev.
- **Decision:** No changes needed. Current architecture is optimal. Developers run `dotnet run --project src/SquadCommerce.AppHost` and get Dashboard at `http://localhost:15902` automatically.
- **Decision doc:** `.squad/decisions/inbox/bill-gates-aspire-local.md` — approved and ready for team consensus

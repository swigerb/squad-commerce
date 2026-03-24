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

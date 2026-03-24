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

<!-- Append new learnings below. Each entry is something lasting about the project. -->

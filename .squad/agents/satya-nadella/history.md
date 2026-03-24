# Project Context

- **Owner:** Brian Swiger
- **Project:** squad-commerce — A sample commerce application demonstrating Microsoft Agent Framework (MAF), A2A, MCP, AG-UI, and A2UI
- **Stack:** ASP.NET Core, SignalR, Blazor (A2UI), C#, Microsoft Agent Framework, MCP, A2A, AG-UI
- **Created:** 2026-03-24

## Core Context

Lead developer for squad-commerce. Responsible for MAF agent orchestration, A2A protocol integration, MCP server/client implementation, and ASP.NET Core backend services. This is a Microsoft showcase demonstrating best practices in AI development.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-24: Agent Projects Scaffolded

**What:** Scaffolded three core agent-related projects for Squad-Commerce

**Projects Created:**
1. **SquadCommerce.Agents** — MAF agent implementations
   - `src/SquadCommerce.Agents/Orchestrator/ChiefSoftwareArchitectAgent.cs` — Orchestrator using MAF Graph-based Workflow
   - `src/SquadCommerce.Agents/Orchestrator/RetailWorkflow.cs` — Workflow definition with nodes and edges
   - `src/SquadCommerce.Agents/Domain/InventoryAgent.cs` — Read-only inventory queries (MCP)
   - `src/SquadCommerce.Agents/Domain/PricingAgent.cs` — Pricing calculations and updates (MCP)
   - `src/SquadCommerce.Agents/Domain/MarketIntelAgent.cs` — Competitor intelligence (A2A)
   - `src/SquadCommerce.Agents/Policies/AgentPolicy.cs` — Immutable policy record
   - `src/SquadCommerce.Agents/Policies/AgentPolicyRegistry.cs` — Central policy registry for all 4 agents
   - `src/SquadCommerce.Agents/Policies/PolicyEnforcementFilter.cs` — MAF filter for runtime policy enforcement
   - `src/SquadCommerce.Agents/Registration/AgentServiceExtensions.cs` — DI registration extension

2. **SquadCommerce.Mcp** — MCP server, tools, and repositories
   - `src/SquadCommerce.Mcp/Tools/GetInventoryLevelsTool.cs` — MCP tool for inventory queries
   - `src/SquadCommerce.Mcp/Tools/UpdateStorePricingTool.cs` — MCP tool for price updates
   - `src/SquadCommerce.Mcp/Data/InventoryRepository.cs` — In-memory repository with demo data (5 stores, 8 SKUs, 40 inventory records)
   - `src/SquadCommerce.Mcp/Data/PricingRepository.cs` — In-memory repository with realistic pricing and margins (5 stores, 8 SKUs, 40 pricing records)
   - `src/SquadCommerce.Mcp/McpServerSetup.cs` — DI registration extension

3. **SquadCommerce.A2A** — A2A protocol implementation
   - `src/SquadCommerce.A2A/AgentCard.cs` — Agent Card definition and factory methods
   - `src/SquadCommerce.A2A/A2AClient.cs` — Client for querying external vendor agents
   - `src/SquadCommerce.A2A/A2AServer.cs` — Server for handling incoming A2A requests
   - `src/SquadCommerce.A2A/Validation/ExternalDataValidator.cs` — Validates external data against internal sources

**Patterns:**
- All agents have meaningful stubs with XML docs explaining their role, allowed tools, and required scopes
- Policy enforcement is immutable and centralized (AgentPolicyRegistry)
- Orchestrator delegates only — never calls MCP tools directly
- Demo data is realistic: 5 stores (Seattle, Portland, SF, LA, Denver), 8 SKUs (tech peripherals), prices with margins
- All three projects build successfully with Contracts project reference
- TODO comments indicate where MAF and MCP NuGet packages will be integrated

**Dependencies:**
- Microsoft.Extensions.Logging.Abstractions 10.0.5
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.5
- Project references: All three projects → Contracts

**File Paths:**
- Agents project: `src/SquadCommerce.Agents/`
- MCP project: `src/SquadCommerce.Mcp/`
- A2A project: `src/SquadCommerce.A2A/`

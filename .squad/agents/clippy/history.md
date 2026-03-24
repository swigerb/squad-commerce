# Project Context

- **Owner:** Brian Swiger
- **Project:** squad-commerce — A sample commerce application demonstrating Microsoft Agent Framework (MAF), A2A, MCP, AG-UI, and A2UI
- **Stack:** ASP.NET Core, SignalR, Blazor (A2UI), C#, Microsoft Agent Framework, MCP, A2A, AG-UI
- **Created:** 2026-03-24

## Core Context

User Advocate and AG-UI Expert for squad-commerce. Responsible for Blazor frontend, A2UI components that render agent responses, AG-UI protocol client implementation, and ensuring the entire user experience is polished, accessible, and showcase-worthy.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-24: Blazor Web Frontend Scaffolding

**What was done:**
- Scaffolded `src/SquadCommerce.Web` project using `dotnet new blazor -n SquadCommerce.Web -o src/SquadCommerce.Web --interactivity Server --framework net10.0`
- Created A2UI component architecture:
  - `Components/A2UI/A2UIRenderer.razor` — Dispatcher component using switch expression on `RenderAs` field
  - `Components/A2UI/RetailStockHeatmap.razor` — Inventory heatmap with color-coded cells (red/yellow/green based on % of target)
  - `Components/A2UI/PricingImpactChart.razor` — Pricing impact cards showing current → proposed price flows and margin deltas
  - `Components/A2UI/MarketComparisonGrid.razor` — Sortable competitor comparison table with trend indicators
- Created chat/streaming components:
  - `Components/Chat/AgentChat.razor` — Real-time streaming chat panel with A2UI rendering support
  - `Components/Chat/AgentStatusBar.razor` — Real-time status updates and urgency badges
- Created services:
  - `Services/AgUiStreamService.cs` — AG-UI SSE client that consumes `/api/agui` endpoint and parses streamed JSON chunks
  - `Services/SignalRStateService.cs` — SignalR client for background state updates (status and urgency badges)
- Added project reference: Web → Contracts
- Added package: `Microsoft.AspNetCore.SignalR.Client` version 10.0.5
- Updated `Components/_Imports.razor` to include A2UI and Chat component namespaces

**Key patterns:**
- A2UIPayload structure: `{ Type, RenderAs, Data }` where Data is parsed as JsonElement for flexible schema
- Components parse JSON data on-the-fly using `JsonElement.TryGetProperty()` for flexible schema support
- All components include proper accessibility (aria-labels, roles, semantic HTML)
- AgentChat supports both text streaming and A2UI payload rendering in the same message flow
- SignalR uses event-based pattern (OnStatusUpdate, OnUrgencyBadge) for loose coupling

**Build status:** ✅ Successful compilation with 1 warning (CA2024 on StreamReader.EndOfStream — non-critical)

**Dependencies:**
- Requires `SquadCommerce.Contracts` project with `A2UIPayload` type
- Requires API endpoint at `/api/agui` for AG-UI streaming (not yet implemented)
- Requires SignalR hub at `/hubs/state` for background updates (not yet implemented)

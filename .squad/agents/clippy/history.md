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

### 2026-03-24: Phase 4 Full Frontend Implementation

**What was done:**
- **Fully implemented all A2UI components** with real data binding to contract types:
  - `RetailStockHeatmap.razor` — Interactive table with sortable stores, color-coded rows (critical/low/good), percentage bars, status badges
  - `PricingImpactChart.razor` — Scenario comparison cards with price flow visualization, margin/revenue metrics, selection callbacks
  - `MarketComparisonGrid.razor` — Sortable competitor table with delta calculations, verification badges, position summary cards
  - All components now deserialize typed data (`RetailStockHeatmapData`, `PricingImpactChartData`, `MarketComparisonGridData`) from JSON
  
- **Enhanced AgentChat** with production-ready features:
  - Auto-scroll to latest messages
  - Connection status indicator (connected/streaming/disconnected)
  - Error handling with user-friendly messages
  - Message formatting (bold text, line breaks)
  - Keyboard shortcuts (Enter to send)
  - Welcome message on init
  - Status message rendering
  - Typing indicator during streaming

- **Enhanced AgentStatusBar** with pipeline progress:
  - Real-time connection state tracking
  - Dismissible urgency badges with auto-cleanup
  - Pipeline progress visualization (4-step workflow)
  - Progress bar with color coding
  - Auto-hide completed pipelines after 3 seconds

- **Created ApprovalPanel** component:
  - Manager approval workflow (Approve All / Modify / Reject All)
  - Confirmation dialogs before actions
  - Processing indicators
  - Success/error result messages
  - POST to `/api/pricing/approve` and `/api/pricing/reject`

- **Redesigned MainLayout** for dual-panel layout:
  - Header with branding and status bar
  - Left sidebar: AgentChat (400px fixed width)
  - Right main area: Dashboard with A2UI components
  - Professional gradient header (purple theme)

- **Enhanced services** with production features:
  - `AgUiStreamService`: Better event type handling (text_delta, tool_call, status_update, a2ui_payload, done), structured logging, resource cleanup
  - `SignalRStateService`: Auto-reconnect with exponential backoff, connection state tracking, multiple event types (StatusUpdate, UrgencyUpdate, A2UIPayload, Notification), graceful degradation if server unavailable

- **Registered services in DI**:
  - `AddHttpClient<AgUiStreamService>` with 5-minute timeout for streaming
  - `AddSingleton<SignalRStateService>` for persistent connection
  - Configured base URL via configuration

- **Created comprehensive CSS** (`app.css` with modern styling):
  - Flexbox/Grid layouts for responsive design
  - Gradient headers and buttons
  - Smooth transitions and hover states
  - Color-coded status indicators (danger/warning/success)
  - Animated typing indicators and status spinners
  - Accessible color contrast ratios
  - Mobile-friendly responsive breakpoints

**Key patterns implemented:**
- SSE event parsing: `data: {json}` format with `[DONE]` marker
- SignalR automatic reconnection with retry policy
- Typed data deserialization from `JsonElement` to contract types
- Event-driven state updates with `EventCallback` and `StateHasChanged`
- Async disposal pattern for SignalR cleanup
- Pipeline progress inference from status messages
- Confirmation dialog pattern for destructive actions

**Build status:** ✅ Clean build with 1 non-critical warning (CA2024)

**Showcase features:**
- Professional UI with modern gradient design
- Real-time streaming with visual indicators
- Interactive components (sortable, hoverable, clickable)
- Full accessibility support (ARIA labels, keyboard navigation)
- Smooth animations and transitions
- Error boundaries and graceful degradation
- Auto-scroll chat with message history
- Pipeline progress visualization
- Manager approval workflow with confirmation

**Next steps required by other agents:**
- API team must implement `/api/agui` SSE endpoint
- API team must implement SignalR hub at `/hubs/agent`
- API team must implement `/api/pricing/approve` and `/api/pricing/reject` endpoints
- Agent team must emit A2UI payloads in correct format
- Integration testing with real agent workflows

### 2026-03-24: Comprehensive Demo Documentation

**What was done:**
- Created `docs/DEMO.md` — A complete step-by-step demo walkthrough for Squad-Commerce
- Updated `README.md` to link to the demo guide with highlights

**Documentation Structure:**
- **Prerequisites:** .NET 10 SDK, Aspire workload, Docker Desktop requirements
- **Quick Start:** Clone, restore, run commands with expected startup behavior and URLs
- **Demo Walkthrough:** Complete competitor price drop scenario with 5 detailed steps:
  1. Trigger Analysis — POST to `/api/agents/analyze` with realistic SKU-1001 (Wireless Mouse) example
  2. Watch AG-UI Stream — SSE event types with example payloads and timeline (status_update, tool_call, a2ui_payload, text_delta, done)
  3. Review A2UI Components — Detailed explanations of RetailStockHeatmap, PricingImpactChart, MarketComparisonGrid with visual examples
  4. Manager Decision — Approve/Reject/Modify workflows with copy-paste curl commands
  5. Verify in Aspire Dashboard — Traces, Metrics, and Logs navigation with real examples
- **API Reference:** Complete endpoint documentation with request/response examples for all agent and pricing endpoints
- **Architecture Overview:** System diagram reference, project structure table, agent flow diagram, protocol usage breakdown
- **Demo Data:** 5 stores (SEA-001 through DEN-005), 8 SKUs (SKU-1001 through SKU-1008), sample inventory and pricing data with instructions for modification
- **Troubleshooting:** 8 common issues with step-by-step solutions (agents not registered, SignalR connection, AG-UI stream, MCP tools, Aspire dashboard, A2UI rendering, pricing validation, slow execution)

**Key documentation patterns:**
- All curl commands are copy-paste ready for PowerShell and Bash
- Real demo data used in examples (actual SKUs, stores, prices from InventoryRepository and PricingRepository)
- Professional formatting for executive audiences (tables, code blocks, visual hierarchy)
- AG-UI event timeline shows exact timing and event flow (0ms → 3700ms)
- Aspire Dashboard sections include real metric names and example queries
- Troubleshooting includes both symptoms and step-by-step solutions
- Cross-references to architecture diagram at docs/squad-commerce-architecture.png

**README updates:**
- Simplified prerequisites (removed SQL Server and GitHub CLI — not needed for demo)
- Added prominent link to DEMO.md with feature highlights
- Highlighted key demo features (✅ checkmarks for scanability)

**Audience focus:**
- Written for executives and technical audiences
- Professional tone, clear structure, visual elements
- Emphasizes Microsoft technologies (MAF, A2A, MCP, AG-UI, A2UI, Aspire, OpenTelemetry)
- Showcase-worthy presentation of agent orchestration capabilities

**File locations:**
- Demo guide: `docs/DEMO.md`
- Updated README: `README.md`
- Referenced diagram: `docs/squad-commerce-architecture.png`

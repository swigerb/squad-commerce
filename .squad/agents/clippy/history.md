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

### 2026-03-24: Decision Audit Trail & Pipeline Visualizer A2UI Components

**What was done:**
- **Created two showcase A2UI components** for Microsoft demo:
  - `DecisionAuditTrail.razor` — Timeline-based audit log showing every action during analysis workflows
  - `AgentPipelineVisualizer.razor` — Horizontal pipeline diagram with real-time stage progress
  
- **Component Features:**
  - **DecisionAuditTrail**:
    - Vertical timeline with top-to-bottom flow (newest first)
    - Each entry shows: timestamp, agent name with emoji, action, protocol badge, duration, status indicator
    - Expandable details: trace ID, affected SKUs/stores, decision outcomes, error messages
    - Color-coded status borders (green=success, red=failed, yellow=warning)
    - Session-based grouping with entry count summary
    - Keyboard navigable with Tab/Enter for expand/collapse
  - **AgentPipelineVisualizer**:
    - Horizontal stage cards with animated status indicators
    - Progress bar showing overall completion percentage
    - Each stage displays: order, agent name, protocol, status (Pending/Running/Completed/Failed/Skipped)
    - Animated connectors between stages with data flow visualization
    - Stage details: duration, tools used, output payloads
    - Pulsing animation for running stages
    - Responsive layout (stacks vertically on narrow screens)
  
- **Updated A2UIRenderer** to dispatch new component types:
  - Added case for "DecisionAuditTrail"
  - Added case for "AgentPipelineVisualizer"
  
- **Data Contracts** (created by Satya in parallel):
  - `DecisionAuditTrailData.cs` with `AuditEntry` records
  - `AgentPipelineData.cs` with `PipelineStage` records
  - All contracts use immutable records with required properties
  - Includes optional fields for trace IDs, affected resources, error messages

**Visual Design:**
- **Purple theme** matching existing components (#b794f4 primary, #9f7aea secondary)
- **Dark mode optimized** (#1a1a1a background, #2d2d2d cards, #e0e0e0 text)
- **Protocol badges** color-coded (MCP=blue, A2A=green, AGUI=purple, Internal=gray)
- **Smooth CSS animations** (no JavaScript):
  - Pulse animation for running stages
  - Shimmer effect on progress bars
  - Flow animation on active connectors
  - Fade-in for expanded details
  - Hover effects with shadow and transform
- **Print-friendly** styles for executive reports
- **Accessibility**: ARIA labels, keyboard navigation, semantic HTML, color + icon for status

**Technical Implementation:**
- JSON deserialization from `JsonElement` with fallback to typed data
- HashSet for tracking expanded entries in audit trail
- LINQ for calculating progress percentages and filtering stages
- TimeSpan formatting helpers for duration display
- String matching for agent emoji assignment
- CSS Grid for stage card layout
- Flexbox for responsive header/stat layouts

**Key Patterns:**
- Component accepts `A2UIPayload` parameter (standard for all A2UI components)
- Dual deserialization path: JsonElement → typed record OR direct typed data
- Click handlers for interactive elements (expand/collapse, stage selection)
- Status-based CSS class mapping for visual indicators
- Relative time calculation from session start or generation time
- Responsive breakpoints at 768px and 1200px

**Build Status:** ✅ Clean build with 1 non-critical warning (CA2024 in AgUiStreamService)

**Showcase Features:**
- Enterprise-grade visual polish for Microsoft executives
- Real-time workflow transparency with audit trail
- Agent orchestration visibility with pipeline stages
- Protocol interaction tracking (MCP, A2A, AGUI)
- OpenTelemetry integration via trace IDs
- Manager decision tracking with outcomes
- Animated stage transitions showing data flow
- Responsive design for mobile/tablet/desktop
- Keyboard accessible for WCAG compliance

**Next Steps:**
- Agents must emit `DecisionAuditTrailData` and `AgentPipelineData` payloads
- API team must wire up A2UIRenderer to agent responses
- Integration testing with real workflow executions
- Demo script creation showing audit trail + pipeline in action


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

### 2026-03-25: AgUiStreamService Two-Step Chat Bridge Flow

**What was done:**
- Refactored `StreamAgUiAsync` from a single-step POST (to GET-only `/api/agui`) to a proper two-step flow:
  1. POST to `/api/agui/chat` with `{ message }` → receives `{ sessionId, streamUrl }` (202 Accepted)
  2. GET `/api/agui?sessionId={sessionId}` to subscribe to the SSE stream
- Added immediate "Connecting to agent stream..." status feedback so the UI isn't blank during handoff
- Added 500ms delay between POST and GET to let background orchestration start writing events
- Added graceful error handling: if chat bridge returns non-success, shows error text in chat instead of crashing
- All existing SSE parsing logic (a2ui, text_delta, status_update, tool_call, done) preserved unchanged

**Key patterns:**
- Two-step bridge: POST for session creation → GET for SSE subscription (matches Bill Gates' decision in `bill-gates-chat-driven-ui.md`)
- Separate `HttpResponseMessage` lifecycle for chat response vs stream response (chat response consumed and discarded before stream starts)
- Stream resources (response, stream, reader) managed in try/finally block as before

**Build status:** ✅ Clean build, 1 pre-existing warning (CA2024). All 13 Web unit tests pass.

**Dependencies:**
- Requires Satya's `POST /api/agui/chat` endpoint on the API side (being implemented in parallel)
- Existing `GET /api/agui?sessionId=` endpoint unchanged

### 2026-03-25: Critical Interactivity Fix — Send Button & Action Cards

**Problem:**
Brian reported that the Send button does NOTHING when clicked, and the "Try These Commands" action cards are not clickable. The web app looked good but was completely non-functional.

**Root Cause:**
In .NET 10 Blazor, without an explicit render mode, components render as **static SSR** — meaning `@bind`, `@onsubmit`, `@onclick`, `@onkeydown` are all inert. The `AgentChat` component lives in `MainLayout.razor` which had no render mode. Only `Home.razor` had `@rendermode InteractiveServer`, but that only affects the page body — NOT layout components (AgentChat, AgentStatusBar).

**Fix Applied:**

1. **App.razor** — Added `@rendermode="InteractiveServer"` to `<Routes />` component. This makes the ENTIRE app interactive via a single SignalR circuit. All components (layout + pages) now have working event handlers.

2. **Home.razor** — Removed redundant `@rendermode InteractiveServer` (now inherited from Routes level). Added `@onclick` handlers to all three action cards that send commands via `ChatCommandService`. Added `role="button"` and `tabindex="0"` for accessibility.

3. **Created `ChatCommandService.cs`** — Simple event-based service enabling cross-component communication between action cards (Home.razor) and chat panel (AgentChat.razor).

4. **Program.cs** — Registered `ChatCommandService` as singleton in DI.

5. **AgentChat.razor** — Injected `ChatCommandService`, subscribed to `OnCommandRequested` events in `OnInitializedAsync`, implemented `IDisposable` for cleanup. When a command arrives from an action card, it auto-populates the input and sends the message.

**Key Learning:**
In .NET 10 Blazor with per-page/component render modes, ANY interactive component that lives in a layout MUST have the render mode set at or above the Routes level. Setting it only on individual pages does NOT propagate to layout components. This is the single most common "dead button" bug in Blazor apps.

**Build status:** ✅ Clean build (0 warnings, 0 errors). All 13 Web unit tests pass.

### 2026-03-26: Phase 1 Items 1.1 + 1.3 + 1.4 — Agentic Command Center Visual Overhaul

**What was done:**

- **Item 1.1 — Fluent UI Blazor + Dark Theme:**
  - Added `Microsoft.FluentUI.AspNetCore.Components` v4.14.0 NuGet package
  - Registered `AddFluentUIComponents()` in Program.cs with proper using directive
  - Added Fluent UI reboot CSS in App.razor `<head>`
  - Added `@using Microsoft.FluentUI.AspNetCore.Components` to `_Imports.razor`
  - Wrapped `<Routes>` in `<FluentDesignSystemProvider BaseLayerLuminance="0.1f">` for dark mode
  - Added Fluent UI web components JS script in App.razor
  - Applied `dark-theme` CSS class to `<body>` with `#0d1117` background
  - Converted sidebar, content areas, header from light (#f8f9fa/white) to dark theme colors (#161b22, #1c2128, #0d1117)
  - Updated header gradient from solid to subtle translucent overlay

- **Item 1.3 — Glassmorphism Card System:**
  - Created `Components/Layout/CommandCard.razor` shared component with `Title`, `CssClass`, and `ChildContent` parameters
  - Created `CommandCard.razor.css` scoped CSS with glassmorphism effect: `backdrop-filter: blur(20px)`, semi-transparent background, subtle border glow on hover
  - Updated `Home.razor` to wrap action cards section, features section, and tech stack in CommandCard components
  - All inner cards (action-card, feature-item, tech-badge) restyled for dark theme with translucent backgrounds

- **Item 1.4 — Agent Persona Avatars + Thinking Animation:**
  - Added 4 agent personas to AgentStatusBar: 🏗️ Orchestrator, 📦 Inventory, 💰 Pricing, 📊 Market Intel
  - Added `IsAgentActive()` method that maps status message keywords to active agents
  - Active agents get highlighted with indigo border glow and elevated brightness
  - Added CSS-animated thinking dots (3 bouncing dots with staggered animation-delay) that appear on active agents
  - Personas shown as pill-shaped badges in the header bar

**Key patterns:**
- `FluentDesignSystemProvider` with `BaseLayerLuminance="0.1f"` sets Fluent UI to dark mode
- Glassmorphism uses `rgba(255,255,255,0.05)` background + `backdrop-filter: blur(20px)` + translucent borders
- Agent activity detection uses keyword matching on status messages (e.g., "inventory" → InventoryAgent active)
- `@@keyframes` (double @) required in Blazor `<style>` blocks for CSS animations
- Scoped CSS on CommandCard means the glassmorphism styles are automatically isolated

**Dark theme color palette:**
- Background: `#0d1117` (GitHub dark)
- Surface: `#161b22` (sidebar), `#1c2128` (headers)
- Text: `#e6edf3` (primary), `rgba(255,255,255,0.5)` (secondary)
- Accent: `#667eea` (indigo), `#764ba2` (purple)
- Borders: `rgba(255,255,255,0.08)` (subtle), `rgba(102,126,234,0.3)` (accent)

**Build status:** ✅ Clean build, 0 errors. All 67 unit tests pass (13 Web + 30 MCP + 24 A2A). Playwright tests are pre-existing failures (require browser environment).

**Files changed:**
- `src/SquadCommerce.Web/SquadCommerce.Web.csproj` — Added Fluent UI NuGet package
- `src/SquadCommerce.Web/Program.cs` — Added `using` + `AddFluentUIComponents()`
- `src/SquadCommerce.Web/Components/App.razor` — Fluent UI CSS, FluentDesignSystemProvider, dark body class, JS script
- `src/SquadCommerce.Web/Components/_Imports.razor` — Added Fluent UI using
- `src/SquadCommerce.Web/Components/Layout/CommandCard.razor` — NEW glassmorphism card component
- `src/SquadCommerce.Web/Components/Layout/CommandCard.razor.css` — NEW scoped CSS
- `src/SquadCommerce.Web/Components/Layout/MainLayout.razor.css` — Dark error UI
- `src/SquadCommerce.Web/Components/Chat/AgentStatusBar.razor` — Agent personas + thinking dots
- `src/SquadCommerce.Web/Components/Pages/Home.razor` — CommandCard wrappers + dark theme restyling
- `src/SquadCommerce.Web/wwwroot/app.css` — Dark theme base colors, removed broken squad-commerce.css import

### 2026-03-26: Phase 1 Items 1.2 + 1.5 + 1.6 + 1.8 — Command Center Layout & Component Polish

**What was done:**

- **Item 1.2 — Convert MainLayout to Fluent Shell:**
  - Replaced raw HTML `<div class="app-layout">` with `<FluentStack Orientation="Vertical">` for semantic flex layout
  - Replaced `<header>` with `<FluentHeader>` for proper Fluent UI header semantics
  - Replaced `<div class="app-content">` with `<FluentStack Orientation="Horizontal">` for the content area
  - Used `<FluentLabel>` with `Weight="FontWeight.Bold"` for header text and section titles
  - Kept 3-column structure (sidebar chat, main content, status bar) intact
  - Fixed stray `}` CSS syntax error in app.css (line 150)

- **Item 1.5 — Polish A2UI Components:**
  - **MarketComparisonGrid:** Replaced plain delta text with `<FluentBadge>` components color-coded green (▲ our advantage), red (▼ competitor advantage), gray (– equal). Wrapped entire component in `<CommandCard>` glassmorphism.
  - **PricingImpactChart:** Added bold monospace styling for metric values (`.metric-value-highlight`), margin delta indicators showing percentage-point changes (▲/▼ with green/red coloring), text-shadow glow for emphasis. Wrapped in `<CommandCard>`.
  - **RetailStockHeatmap:** Added color-coded quantity cells (red=critical, amber=low, green=good), enhanced status indicators with pill-shaped badges per status level, color-coded progress bars, improved legend with emoji. Wrapped in `<CommandCard>`.

- **Item 1.6 — Pipeline Progress Animation:**
  - Added `stageSlideIn` CSS animation: stages slide in from left with staggered delays (0s, 0.15s, 0.3s, ...) for up to 6 stages
  - Enhanced `pulseGlow` animation for active/running stages: dual box-shadow glow with pulsing intensity
  - Enhanced completed stage styling: green gradient background, green-tinted stage number circle, ✅ checkmark overlay via CSS `::after` pseudo-element
  - Added `position: relative` to stage-card for absolute positioning of checkmark
  - Failed stages now have subtle red background tint

- **Item 1.8 — Integrate ThinkingState in AgentStatusBar:**
  - Added `OnThinkingState` event to `SignalRStateService` with `(string sessionId, string agentName, bool isThinking)` signature
  - Registered `ThinkingState` SignalR handler in the hub connection alongside existing handlers
  - Added `_thinkingAgents` HashSet (case-insensitive) to AgentStatusBar for tracking which agents are thinking
  - `HandleThinkingState` normalizes agent names to match persona dictionary keys (handles suffix variations like "Agent")
  - Updated `IsAgentActive()` to prioritize real ThinkingState from SignalR, falling back to status-text heuristic for backward compatibility
  - Properly subscribes/unsubscribes to `OnThinkingState` in lifecycle methods

**Key patterns:**
- FluentStack with `Style="gap: 0;"` prevents unwanted spacing between layout sections
- FluentHeader with `Style="min-height: auto; height: auto;"` prevents fixed height
- FluentBadge with CSS class overrides for custom colors (shadow DOM won't block class-based styling)
- CommandCard wrapping pattern: wrap component content without Title param to avoid double headers
- Agent name normalization: case-insensitive matching + "Agent" suffix stripping for flexible backend integration
- CSS `::after` pseudo-element for checkmark overlay on completed pipeline stages

**Build status:** ✅ Clean build, 0 errors. All 13 Web unit tests pass. Playwright tests are pre-existing failures (require running app server).

**Files changed:**
- `src/SquadCommerce.Web/Components/Layout/MainLayout.razor` — FluentStack + FluentHeader + FluentLabel conversion
- `src/SquadCommerce.Web/wwwroot/app.css` — Fixed stray brace, added header-icon style
- `src/SquadCommerce.Web/Services/SignalRStateService.cs` — Added OnThinkingState event + handler registration
- `src/SquadCommerce.Web/Components/Chat/AgentStatusBar.razor` — ThinkingState integration with agent tracking
- `src/SquadCommerce.Web/Components/A2UI/MarketComparisonGrid.razor` — FluentBadge deltas + CommandCard wrap
- `src/SquadCommerce.Web/Components/A2UI/PricingImpactChart.razor` — Bold metrics + margin deltas + CommandCard wrap
- `src/SquadCommerce.Web/Components/A2UI/RetailStockHeatmap.razor` — Color-coded quantities + enhanced status + CommandCard wrap
- `src/SquadCommerce.Web/Components/A2UI/AgentPipelineVisualizer.razor` — Slide-in animation + glow + checkmark

### 2026-03-26: Phase 2 Items 2.5 + 2.6 + 2.7 — Command Palette, HITL Fluent Upgrade, Telemetry Dashboard

**What was done:**

- **Item 2.5 — CMD+K Command Palette:**
  - Created `Components/Layout/CommandPalette.razor` — Modal overlay with glassmorphism backdrop, search input, filtered command list
  - Created `CommandPalette.razor.css` — Dark glassmorphism styling with slide-in animation, keyboard hints, and selected-state highlighting
  - Created `CommandPalette.razor.js` — JS interop module that registers `Ctrl+K` / `Cmd+K` global keyboard listener, calls `[JSInvokable] Toggle()` via DotNetObjectReference
  - 5 built-in commands: Check Inventory, Analyze Pricing, Compare Market (active), View Pipeline, System Health (disabled/coming-soon)
  - Fuzzy search: splits query into tokens, matches against label + description + agent name
  - Keyboard navigation: ↑↓ to move, Enter to execute, Escape to close
  - Clicking active command sends via `ChatCommandService` and closes palette
  - Auto-focus search input on open
  - Added `<CommandPalette />` to `MainLayout.razor`
  - Implements `IAsyncDisposable` for proper JS module cleanup

- **Item 2.6 — HITL Approval Cards Fluent Upgrade:**
  - Replaced raw HTML in `ApprovalPanel.razor` with Fluent UI components: `FluentCard`, `FluentButton`, `FluentBadge`, `FluentLabel`, `FluentStack`, `FluentIcon`, `FluentProgress`
  - Wrapped entire panel in `<CommandCard>` for glassmorphism
  - Approve button uses `Appearance.Accent`, Modify/Reject use `Appearance.Outline`
  - Reject button has custom red accent styling via CSS override
  - Added risk-level badge with color coding: Green (≤5% margin impact), Yellow (5-15%), Red (>15%)
  - Risk calculated from `_estimatedImpact / (totalChanges * 100)` percentage
  - Confirmation dialog wrapped in `<CommandCard>` glassmorphism
  - All existing approve/reject/modify functionality preserved
  - Created `ApprovalPanel.razor.css` — Scoped styles for risk badges, action buttons, summary card, confirm dialog
  - Fluent icons: CheckmarkCircle (approve), Edit (modify), DismissCircle (reject), Dismiss (close)

- **Item 2.7 — Telemetry Dashboard (Live Metrics Panel):**
  - Created `Components/A2UI/TelemetryDashboard.razor` — 4-card grid showing Agent Invocations, Avg Latency, MCP Tool Calls, A2A Handshakes
  - Each card uses `<CommandCard>` glassmorphism wrapper with large gradient number, label, trend indicator, and progress bar
  - Auto-refresh via `System.Threading.Timer` every 5 seconds with incremental mock data
  - Trend indicators: ▲ 12% (invocations), ▼ 8% (latency/good), ▲ 5% (tools), — 0% (handshakes)
  - Gradient progress bars: purple (invocations), green (latency), blue (tools), violet (handshakes)
  - Created `TelemetryDashboard.razor.css` — Metric card grid, gradient numbers, trend badges, animated bar fills
  - Added "🩺 System Health" section with pulsing "● Live" indicator to `Home.razor`
  - Rendered `<TelemetryDashboard />` on dashboard between Quick Actions and Features sections
  - Implements `IDisposable` for timer cleanup

- **Dependencies Added:**
  - `Microsoft.FluentUI.AspNetCore.Components.Icons` v4.14.0 NuGet package (for `FluentIcon` and `Icons.Regular.Size20.*`)
  - `@using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons` added to `_Imports.razor`

**Key patterns:**
- Blazor component attributes with mixed C#/markup require `@($"static-class {DynamicMethod()}")` interpolation syntax
- `Icons` in Fluent UI v4 requires a separate NuGet package and a `using` alias directive
- JS interop via ES modules: `import("./Components/Layout/CommandPalette.razor.js")` with `DotNetObjectReference` for callbacks
- `System.Threading.Timer` with `InvokeAsync(StateHasChanged)` for safe Blazor thread-marshal on background updates
- Risk-level thresholds: ≤5% = Low/Green, 5-15% = Medium/Yellow, >15% = High/Red

**Build status:** ✅ Clean build, 0 errors, 0 warnings. Playwright tests are pre-existing failures (require running app server).

**Files created:**
- `src/SquadCommerce.Web/Components/Layout/CommandPalette.razor` — CMD+K command palette component
- `src/SquadCommerce.Web/Components/Layout/CommandPalette.razor.css` — Palette glassmorphism styles
- `src/SquadCommerce.Web/Components/Layout/CommandPalette.razor.js` — Ctrl+K keyboard shortcut listener
- `src/SquadCommerce.Web/Components/Chat/ApprovalPanel.razor.css` — Approval panel Fluent styles
- `src/SquadCommerce.Web/Components/A2UI/TelemetryDashboard.razor` — Live metrics dashboard
- `src/SquadCommerce.Web/Components/A2UI/TelemetryDashboard.razor.css` — Metrics card styles

**Files modified:**
- `src/SquadCommerce.Web/Components/Layout/MainLayout.razor` — Added `<CommandPalette />`
- `src/SquadCommerce.Web/Components/Chat/ApprovalPanel.razor` — Full Fluent UI conversion with risk badges
- `src/SquadCommerce.Web/Components/Pages/Home.razor` — Added System Health section + TelemetryDashboard
- `src/SquadCommerce.Web/Components/_Imports.razor` — Added Icons alias
- `src/SquadCommerce.Web/SquadCommerce.Web.csproj` — Added Fluent UI Icons package

### 2026-03-24: Agent Fleet Pulse Sidebar Panel (Phase 2, Item 2.1)

**What was done:**
- Created `Components/Chat/AgentFleetPanel.razor` — Rich sidebar panel showing 4 agent cards with real-time status
- Created `Components/Chat/AgentFleetPanel.razor.css` — Scoped CSS with glassmorphism, pulse/glow animations, dark theme
- Integrated panel into `MainLayout.razor` as a right sidebar alongside chat and dashboard
- Added `.sidebar-right` styles to `wwwroot/app.css`

**Component features:**
- 4 agent cards: ChiefSoftwareArchitect (🏗️ Orchestrator), InventoryAgent (📦), PricingAgent (💰), MarketIntelAgent (📊)
- Real-time status: 🟢 Idle, 🔵 Thinking (pulse), 🔴 Error — driven by SignalR `ThinkingState` events
- Last-action text updated from `ReasoningStep` events and `StatusUpdate` heuristics
- Protocol badges (MCP, A2A, AG-UI) with color-coded pill styling
- CSS `@keyframes` pulse/glow animations on active agents
- Each card wrapped in `CommandCard` (glassmorphism) with status-colored borders
- SignalR connection indicator in panel header

**Key patterns:**
- Reuses `ResolveAgentKey()` pattern from AgentStatusBar for normalizing agent names
- Subscribes to `OnThinkingState`, `OnReasoningStep`, and `OnStatusUpdate` from `SignalRStateService`
- Truncates last-action text to 60 chars with ellipsis
- `::deep` selectors used to style `CommandCard` children from scoped CSS

**Build status:** ✅ 0 warnings, 0 errors

**Files created:**
- `src/SquadCommerce.Web/Components/Chat/AgentFleetPanel.razor`
- `src/SquadCommerce.Web/Components/Chat/AgentFleetPanel.razor.css`

**Files modified:**
- `src/SquadCommerce.Web/Components/Layout/MainLayout.razor` — Added right sidebar with `<AgentFleetPanel />`
- `src/SquadCommerce.Web/wwwroot/app.css` — Added `.sidebar-right` layout styles

### 2026-03-25: Phase 2 Items 2.3 + 2.4 — Chain of Thought Panel & Tool Call Timeline

**What was done:**
- **Created `ReasoningTracePanel.razor`** (Item 2.3) — Vertical timeline showing agent reasoning process:
  - Subscribes to `SignalRStateService.OnReasoningStep` for real-time streaming
  - Icon-coded steps: 💭 Thinking, 🔧 ToolCall, 🤝 A2AHandshake, 👁️ Observation, ✅ Decision, ❌ Error
  - Agent name badges with per-agent color coding (8-color palette)
  - Duration badges (ms/s/m format), timestamps in HH:mm:ss.fff
  - Nested steps (via ParentStepId) indented under parents
  - Collapsible long content with gradient fade
  - Slide-down animation on new steps, auto-clears on session change
  - Custom scrollbar styling for dark theme
- **Created `ToolCallTimeline.razor`** (Item 2.4) — Horizontal Gantt-style bar chart:
  - Filters ReasoningStep to only ToolCall and A2AHandshake types
  - Groups bars by agent name, one row per agent
  - Bar width proportional to DurationMs, minimum 60px visible width
  - Color-coded: green for ToolCall, blue for A2AHandshake
  - Hover tooltips with tool name, duration, timestamp
  - Legend, horizontal scroll, bar scale-in animations
  - Pure CSS (no JS charting libraries)
- **Integrated into Home.razor** — 2-column grid layout below System Health section
  - Responsive: stacks to single column under 1024px
  - Section header with "● Live" pulse indicator

**Key patterns:**
- First A2UI components to use **direct SignalR event subscription** (all prior components used Parameter/A2UIPayload binding)
- `IDisposable` pattern to unsubscribe from SignalR events on component teardown
- `InvokeAsync(StateHasChanged)` required because SignalR callbacks arrive on non-UI thread
- Session-aware: auto-clear lists when `SessionId` changes across steps

**Build status:** ✅ Successful — 0 warnings, 0 errors

# Project Context

- **Owner:** Brian Swiger
- **Project:** squad-commerce — A sample commerce application demonstrating Microsoft Agent Framework (MAF), A2A, MCP, AG-UI, and A2UI
- **Stack:** ASP.NET Core, SignalR, Blazor (A2UI), C#, Microsoft Agent Framework, MCP, A2A, AG-UI
- **Created:** 2026-03-24

## Core Context

User Advocate and AG-UI Expert for squad-commerce. Responsible for Blazor frontend, A2UI components that render agent responses, AG-UI protocol client implementation, and ensuring the entire user experience is polished, accessible, and showcase-worthy.

**Frontend Architecture (2026-03-24):**
- Blazor Web Server with Server-side Interactivity (.NET 10.0)
- A2UI Component Library: RetailStockHeatmap, PricingImpactChart, MarketComparisonGrid — all typed via A2UIPayload record with RenderAs discriminator
- Chat/Streaming: AgentChat.razor (SSE streaming), AgentStatusBar.razor (real-time status), ActivityTimeline.razor (agent history)
- Services: AgUiStreamService (AG-UI SSE client), SignalRStateService (background push updates), FleetStateService (agent status aggregation)

**Established Patterns:**
- A2UI components only — no raw markdown for complex data. Contract validation ensures type safety.
- SSE stream (`/api/agui`) is sync request/response for chat; SignalR is async-only for background state
- Client-side bridge pattern: AgentActivityService connects SSE events to UI components
- Dark theme support, accessible typography, responsive panels

**Design System (2026-03-24-03-26):**
- Colors: Alert Red (#F14438), Success Green (#12A24F), Disabled Gray (#C5C5C5)
- Components: Status badges, urgency indicators, activity timeline with color-coded agent states
- Layouts: Fixed header (24px safe area), sidebar (240px collapsible), main content area (flex 1)
- Typography: 14px body, 16px labels, 20px headings, Segoe UI font stack

**Current Status (2026-03-27):**
- Activity Timeline scroll fixed: min-height:0 on flex parent containers
- Agent Fleet panel now shows real-time activity from both SSE and SignalR channels
- AgentActivityService bridges SSE events from chat to fleet panel (no backend changes)
- Web project compiles with 0 errors

## Learnings

### ARCHIVE: Development History 2026-03-24 to 2026-03-26

[Archived 14 learning entries covering:
- Blazor web frontend scaffolding (A2UI components, chat services)
- A2UI component expansion (all 3 renderers + services)
- SignalR integration for background state updates
- Streaming chat with SSE protocol client
- Design system (colors, typography, layouts)
- Activity timeline component with color-coded states
- Agent fleet panel with status aggregation
- Blazor rendering optimization (event binding, UI updates)
- Advanced scenarios (drill-down, real-time data sync)
- Web infrastructure & testing (route guards, loader states)
- Accessibility & dark theme support
- Web project compiling with 0 errors at 2026-03-26

For full historical entries, see git log or .squad/orchestration-log/
]

---

### 2026-03-27: Agent Status Bar — Mission Control Overhaul

**What was done:** Complete rework of the AgentStatusBar header component per Brian's feedback.

**Problems fixed:**
- Agent badges were dead-gray when idle — now have per-agent color theming with subtle standby breathing animation
- "Agents idle" + "Live" text was contradictory — replaced with unified "Status Beacon" showing "System Ready" (idle) or "Processing" (active)
- No pulse/glow effects matching System Health — now uses matching CSS animation patterns (keyframe pulses, box-shadow glow, scale transitions)

**Design decisions:**
- Per-agent CSS custom properties (`--agent-color`, `--agent-rgb`) for Orchestrator (purple), Inventory (green), Pricing (blue-purple), Market Intel (blue)
- Three agent states: `standby` (faint color glow + slow breathe), `active` (bright color + pulse-glow animation + thinking dots)
- Status beacon replaces split status/connection sections — single coherent indicator with 5 states (standby, processing, error, connecting, disconnected)
- Thinking dots inherit agent color via CSS variable — not hardcoded `#667eea`
- All animations use 300ms+ transitions with `cubic-bezier(0.4, 0, 0.2, 1)` easing

**Key files:**
- `src/SquadCommerce.Web/Components/Chat/AgentStatusBar.razor` — full rewrite of markup, CSS, and code-behind
- CSS animations: `standby-breathe` (4s), `agent-pulse` (2s), `beacon-standby` (3s), `beacon-processing` (1.6s)

**User preference:** Brian wants "SpaceX mission control" feel — alive dashboard, not a static status page. System Health pulse effect is the gold standard.

---

### 2026-03-27: Activity Bridge Pattern & Timeline Scroll Fix
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

### 2026-03-25: Phase 3.1 — Interactive Node-Graph Pipeline Visualization

**What was done:**
- Created `PipelineNodeGraph.razor` — A DAG-style SVG node graph showing agent orchestration as nodes and data flow as edges
- Integrated into `Home.razor` above the Agent Reasoning section with "Orchestration Pipeline" header and live badge

**Component Features:**
- **SVG DAG Layout**: 4 agent nodes (Orchestrator at top, MarketIntel/Inventory/Pricing fanned below) connected by animated edges
- **Real-time state**: Subscribes to `SignalRStateService.OnThinkingState` and `OnReasoningStep` to drive node/edge state
- **Status color coding**: idle=gray, thinking=animated-blue with glow, complete=green, error=red
- **Animated data packets**: SVG circles travel along edges via `<animateMotion>` when agents are active
- **Edge animations**: Dashed stroke animation (`edgeFlowDash`) for active connections, solid green for complete
- **Node glow filter**: SVG `feGaussianBlur` filter applied to active nodes for visual emphasis
- **Activity bar**: Bottom chip strip showing agent status with spinners and checkmarks
- **Agent name normalization**: Fuzzy matching handles variations like "ChiefSoftwareArchitectAgent" or "chief-software-architect"
- **Glassmorphism wrapper**: Matches CommandCard styling with backdrop-filter blur
- **Dark theme native**: All colors use the project palette (#667eea, #764ba2, #3fb950, #0d1117)
- **Responsive**: SVG viewBox scales to container width; layout adjusts at 640px breakpoint

**Technical approach:**
- Pure CSS + inline SVG — zero JavaScript, zero external libraries
- `preserveAspectRatio="xMidYMid meet"` ensures the graph scales proportionally
- SVG `<defs>` block contains reusable gradients, glow filters, and flow patterns
- `IDisposable` pattern for SignalR event cleanup
- `InvokeAsync(StateHasChanged)` for thread-safe UI updates from SignalR callbacks

**Build status:** ✅ Clean build — 0 warnings, 0 errors

### 2026-03-25: Phase 3 Items 3.2 + 3.4 + 3.7

**What was done:**

- **Item 3.2 — A2A Handshake Animations:**
  - Added animated pulse dot inside A2A status badges (`a2a-pulse-dot`) in `AgentFleetPanel.razor`
  - Added `a2a-connection-line` with `a2a-data-flow` sliding effect — a subtle data-flowing pulse between agents
  - Colors: blue pulse for negotiating, green flash for completed, red pulse for failed
  - All CSS animations in `AgentFleetPanel.razor.css` using `@@keyframes` (Blazor-escaped)

- **Item 3.4 — Keyboard Shortcut System:**
  - Extended `CommandPalette.razor.js` with `registerKeyboardShortcuts()` — handles 1-4 (panel focus), `/` (chat focus), `?` (help overlay), `Esc` (close)
  - Added `focusChatInput()` and `focusPanel()` JS helpers
  - Shortcuts skip input fields (except Esc) to avoid conflicts with typing
  - Created `KeyboardShortcutHelp.razor` + `.razor.css` — modal listing all shortcuts, dark-themed, accessible
  - Integrated into `MainLayout.razor` via `IAsyncDisposable` lifecycle and `[JSInvokable]` callback

- **Item 3.7 — Responsive / Mobile Layout:**
  - Added media queries in `MainLayout.razor.css`:
    - Below 1200px: vertical stacking of sidebar + main + fleet
    - Below 768px: fleet panel hidden, toggle button appears, touch-friendly 44px hit targets
    - Below 480px: extra compact spacing
  - Added `fleet-toggle-btn` in header (🚀 icon), wired to `ToggleFleetPanel()`
  - `fleet-hidden` CSS class for programmatic toggle
  - Chat input sticky at bottom on mobile, 16px font to prevent iOS zoom
  - All existing functionality preserved

**Key patterns:**
- JS interop module (`CommandPalette.razor.js`) shared between CommandPalette and MainLayout — single source for keyboard handling
- MainLayout now implements `IAsyncDisposable` for proper JS cleanup
- Responsive approach uses CSS media queries + Blazor toggle state for fleet panel
- `@@keyframes` double-@ escaping required in `.razor.css` scoped stylesheets

**Build status:** ✅ Successful compilation

### Phase 3, Item 3.6: Audio Cues for Agent Events

**What was done:**
- Created `Components/Layout/AudioCueService.razor` — invisible Blazor component that subscribes to SignalR events (ThinkingState, A2AHandshakeStatus, StatusUpdate) and triggers Web Audio API tones via JS interop
- Created `Components/Layout/AudioCueService.razor.js` — ES module using OscillatorNode to synthesize tones:
  - `playSweep()` — frequency sweep for agent-start (rising) and error (descending) cues
  - `playChime()` — sequential notes for agent completion (C5→E5) and A2A handshake (C5→E5→G5 arpeggio)
  - `playChord()` — simultaneous notes for all-agents-done (C major chord)
  - `isMuted()`/`setMuted()` — localStorage persistence of mute state
- Updated `MainLayout.razor` — added 🔊/🔇 mute toggle button in header and `<AudioCueService>` component with two-way `@bind-IsMuted` binding
- Updated `MainLayout.razor.css` — styled `.audio-mute-btn` for dark theme consistency

**Key patterns:**
- Default state is **muted** (opt-in experience) — stored in `localStorage` key `squad-audio-muted`
- All volumes kept at 0.05–0.12 range for subtlety; tones < 500ms
- Tracks `_thinkingAgents` HashSet to detect "all agents done" vs single-agent completion
- All audio playback wrapped in try/catch — audio failures are non-critical
- Uses same JS interop module pattern as CommandPalette (ES module import, DisposeAsync cleanup)

**Build status:** ✅ Successful compilation (0 warnings, 0 errors)

### 2026-03-25: Header Dark Theme Fix + Price Regex + Stream Timing

**What was done:**
- **Replaced FluentHeader with plain <header>** in MainLayout.razor — the Fluent UI <fluent-header> web component was injecting its own light blue background via shadow DOM, overriding our dark theme CSS
- **Restructured header into 3-column flex layout:** brand (left) | agent personas via AgentStatusBar (center) | mute/fleet controls (right) — all on one line, no wrapping
- **Added proper flex styles** to AgentStatusBar: lex-wrap: nowrap, white-space: nowrap, lex-shrink: 0 on each section to prevent overflow
- **Hidden pipeline progress bar** from header display (too wide for header row; belongs in a panel)
- **Fixed price regex** in Program.cs chat bridge — old \$?([\d]+\.?\d*)` matched "100" from "SKU-100". New regex uses lookbehind (?<![A-Za-z][-]) to skip SKU-embedded numbers, only matching $-prefixed or standalone decimal prices
- **Increased AG-UI stream delay** from 500ms to 1500ms in AgUiStreamService.cs — gives the background Task.Run orchestration time to write its first event before the GET subscriber connects

**Key patterns:**
- Fluent UI web components have shadow DOM that overrides external CSS — prefer plain HTML elements when full style control is needed
- Header background color: #1a1a2e (dark navy) — matches the command center theme
- Price regex: (?<![A-Za-z][-])\$([\d]+\.?\d*)|(?<![A-Za-z\d][-])\b([\d]+\.\d{2})\b` — uses two alternations: dollar-sign prices and standalone decimals
- Removed dead CSS: old .header-brand h1 and duplicate .header-subtitle rules

**Build status:** ✅ Both Web and API projects compile with 0 warnings, 0 errors

### 2026-07-15: Dashboard Scroll Fix + Agent Fleet Activity Bridge

**What was done:**
- Fixed Analysis Dashboard content being cut off (couldn't scroll to Activity Timeline):
  - Added `min-height: 0` to `.main-content`, `.content-body`, `.sidebar-left`, `.sidebar-right` in app.css — required for nested flex containers to properly constrain overflow
  - Added `flex-shrink: 0` to `.content-header` to prevent it collapsing under flex pressure
  - Added `scrollbar-width: thin` + `scrollbar-color` (Firefox) and `::-webkit-scrollbar` styles (Chrome/Edge) for dark-theme-consistent scrollbars
- Fixed Agent Fleet panel not showing activity when user sends a chat message:
  - Created `Services/AgentActivityService.cs` — lightweight singleton event bus bridging SSE stream events to UI components
  - Modified `AgentChat.razor` to call `ActivityService.NotifyStreamingStarted()`, `NotifyStatusUpdate()`, and `NotifyStreamingCompleted()` during SSE streaming
  - Modified `AgentFleetPanel.razor` to subscribe to `AgentActivityService` events — agents now show "Thinking" status with pulse glow when processing
  - Modified `AgentStatusBar.razor` (header bar) to also subscribe — status bar now shows activity instead of "Agents idle" during requests
  - Registered `AgentActivityService` as singleton in Web `Program.cs`

**Root causes found:**
- Scroll issue: nested flex containers (FluentStack → main-content → content-body) need explicit `min-height: 0` to break the default `min-height: auto` behavior that prevents overflow scrolling
- Agent Fleet issue: the system has two parallel data channels — SSE (chat) and SignalR (background updates). The Agent Fleet only listened to SignalR, but the chat bridge sends status via SSE. Created a client-side bridge to forward SSE events to the fleet panel

**Key file paths:**
- `src/SquadCommerce.Web/wwwroot/app.css` — global layout styles
- `src/SquadCommerce.Web/Services/AgentActivityService.cs` — SSE→UI activity bridge
- `src/SquadCommerce.Web/Components/Chat/AgentFleetPanel.razor` — agent status cards
- `src/SquadCommerce.Web/Components/Chat/AgentStatusBar.razor` — header status bar
- `src/SquadCommerce.Web/Components/Chat/AgentChat.razor` — chat with SSE streaming

**Key patterns:**
- Dual-channel bridging: when SSE and SignalR carry overlapping data, use a shared singleton service as event bus
- Flex scroll fix: always add `min-height: 0` to flex children that need internal scrolling
- Dark-theme scrollbar: use both `scrollbar-width`/`scrollbar-color` (Firefox) and `::-webkit-scrollbar` (Chrome/Edge)

**Build status:** ✅ Web project compiles with 0 errors

---

### 2026-03-27: Activity Bridge Pattern & Timeline Scroll Fix

**What was done:**
- Fixed Activity Timeline scroll cutoff by adding `min-height: 0` to parent flex containers
- Added dark-theme scrollbar styling for ActivityTimeline component
- Created `AgentActivityService` — lightweight client-side event bus singleton
- Bridged SSE stream events from AgentChat to AgentFleetPanel and AgentStatusBar
- Both channels (SSE and SignalR) now independently drive UI state

**Why this matters:**
- Agent Fleet panel was showing "Idle" during active agent processing (only watched SignalR, not SSE)
- Activity Timeline was cutting off content during vertical scroll
- Pattern: New activity-aware UI components should inject both `SignalRStateService` AND `AgentActivityService`
- If SignalR unavailable, UI still receives activity from SSE stream
- If SignalR adds ThinkingState later, both paths work without conflict

**UI Components Updated:**
- `Components/Agent/AgentFleetPanel.razor` — Subscribed to both SignalR and AgentActivityService
- `Components/Chat/AgentStatusBar.razor` — Subscribed to both services
- `Components/Chat/ActivityTimeline.razor` — Fixed flex container overflow with min-height:0

**Files Changed:**
- `src/SquadCommerce.Web/Services/AgentActivityService.cs` — New service (singleton)
- `src/SquadCommerce.Web/Components/Agent/AgentFleetPanel.razor` — Subscriptions added
- `src/SquadCommerce.Web/Components/Chat/AgentStatusBar.razor` — Subscriptions added
- `src/SquadCommerce.Web/Components/Chat/ActivityTimeline.razor` — Flex overflow fix

**Build status:** ✅ Web project compiles with 0 errors

**Decision:** [Agent Activity Bridge Pattern](../../decisions.md#2026-07-15-agent-activity-bridge-pattern)
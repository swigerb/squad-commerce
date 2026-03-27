# Decision: Agent Status Bar — Mission Control Overhaul

**By:** Clippy (User Advocate / AG-UI Expert)
**Date:** 2026-03-27
**Status:** Implemented

## Context

Brian reported three UX issues with the top header agent status bar:
1. Agent badges looked dead/disabled (gray) even when the app was "Live"
2. "Agents idle" + "Live" text was contradictory messaging
3. Wanted the same alive-feeling pulse effects used in the System Health panel

## Decision

Rewrote the AgentStatusBar component with a mission-control aesthetic:

**D1. Per-Agent Color Tokens:** Each agent badge uses CSS custom properties (`--agent-color`, `--agent-rgb`) for its unique color: Orchestrator=purple, Inventory=green, Pricing=blue-purple, Market Intel=blue. This replaces the single hardcoded `#667eea` active color.

**D2. Three-State Badges:** Agent badges have `standby` (faint agent-colored glow, slow breathing animation) and `active` (bright glow, pulse animation, thinking dots). No more dead/disabled look — standby feels like "ready and waiting."

**D3. Unified Status Beacon:** Replaced the split "status section" + "connection section" with a single Status Beacon showing contextual state: "System Ready" (green pulse, idle), "Processing" (blue pulse, agents active), "Error"/"Connecting"/"Offline" for connection issues.

**D4. System Health Animation Parity:** Adopted the same animation patterns from TelemetryDashboard — CSS keyframe pulses, box-shadow glow, scale transforms. Timings: standby-breathe 4s, agent-pulse 2s, beacon-processing 1.6s.

## Impact

- AgentStatusBar.razor — full component rewrite (markup, CSS, code-behind)
- No backend changes, no new service dependencies
- Responsive breakpoint hides labels on narrow screens

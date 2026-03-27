# Decision: Agent Activity Bridge Pattern

**By:** Clippy (User Advocate / AG-UI Expert)
**Date:** 2026-07-15
**Status:** Implemented

## Context

The system has two parallel data channels:
1. **SSE Stream** (`/api/agui`) — synchronous request/response for chat UI
2. **SignalR** (`/hubs/agent`) — asynchronous background push updates

The Agent Fleet panel only subscribed to SignalR events, but the chat bridge sends agent status updates via SSE. This meant the fleet panel showed "Idle" even when agents were actively processing a user request.

## Decision

Created `AgentActivityService` as a lightweight singleton event bus that bridges SSE stream events from `AgentChat` to UI components like `AgentFleetPanel` and `AgentStatusBar`. This is a **client-side bridge** — no backend changes required.

## Pattern

- `AgentChat` → writes to `AgentActivityService` during streaming
- `AgentFleetPanel` → subscribes to both `SignalRStateService` AND `AgentActivityService`
- `AgentStatusBar` → subscribes to both services

## Why This Approach

- Avoids modifying backend code (stays within Clippy's domain)
- Both channels can independently drive the UI
- If SignalR is unavailable, the fleet still shows activity from SSE
- If SignalR starts sending ThinkingState, both paths work without conflict

## Impact

Team members building new UI components that need agent activity state should inject `AgentActivityService` in addition to `SignalRStateService`.

# 🎬 Squad Commerce — Demo Walkthrough

> **A step-by-step guide to experiencing Microsoft's showcase of Agent Framework excellence**

This guide walks you through all five Squad-Commerce demo scenarios — from competitor price drops to ESG audits — demonstrating the power of Microsoft Agent Framework, A2A, MCP, AG-UI, and A2UI working together in a real enterprise application.

---

## 📋 Table of Contents

- [Scenarios Overview](#-scenarios-overview)
- [Prerequisites](#-prerequisites)
- [Quick Start](#-quick-start)
- [Demo Walkthrough: Scenario 1 — Competitor Price Drop](#-demo-walkthrough-competitor-price-drop-scenario)
  - [Step 1: Trigger Analysis](#step-1-trigger-analysis)
  - [Step 2: Watch the AG-UI Stream](#step-2-watch-the-ag-ui-stream)
  - [Step 3: Review A2UI Components](#step-3-review-a2ui-components)
  - [Step 4: Manager Decision](#step-4-manager-decision)
  - [Step 5: Verify in Aspire Dashboard](#step-5-verify-in-aspire-dashboard)
- [Demo Walkthrough: Scenario 2 — Viral Spike](#-demo-walkthrough-scenario-2--viral-spike)
- [Demo Walkthrough: Scenario 3 — Supply Chain Shock](#-demo-walkthrough-scenario-3--supply-chain-shock)
- [Demo Walkthrough: Scenario 4 — Store Readiness](#-demo-walkthrough-scenario-4--store-readiness)
- [Demo Walkthrough: Scenario 5 — ESG Audit](#-demo-walkthrough-scenario-5--esg-audit)
- [API Reference](#-api-reference)
- [Architecture Overview](#-architecture-overview)
- [Demo Data](#-demo-data)
- [Troubleshooting](#-troubleshooting)

---

## 🗺️ Scenarios Overview

Squad Commerce ships with **5 end-to-end demo scenarios**, each representing a real business challenge that multi-agent orchestration solves in minutes instead of days.

| # | Scenario | Business Challenge | Agents | Key A2UI |
|---|----------|-------------------|--------|----------|
| 1 | Competitor Price Drop | Protect margins while staying competitive | MarketIntel → Inventory → Pricing | RetailStockHeatmap, PricingImpactChart, MarketComparisonGrid |
| 2 | Viral Spike | Capitalize on viral demand without stockouts | MarketIntel → Pricing → Marketing | SocialSentimentGraph, CampaignPreview, PricingImpactChart |
| 3 | Supply Chain Shock | Minimize stockout risk through redistribution | Logistics → Inventory → Redistribution | ReroutingMap, RetailStockHeatmap, RiskScoreGauge |
| 4 | Store Readiness | Optimize layouts using traffic data | TrafficAnalyst → Merchandising → Manager | InteractiveFloorplan (HITL) |
| 5 | ESG Audit | Proactive compliance risk management | Compliance → Research → Procurement | SupplierRiskMatrix |

> **Tip:** Each scenario is triggered by natural language in the chat UI. The orchestrator detects the intent and routes to the correct agent pipeline automatically. See [Scenario Detection Keywords](#scenario-detection-keywords) in the API Reference for the full list.

---

## 🔧 Prerequisites

Before running the demo, ensure you have the following installed:

### Required Software

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download)** (version 10.0 or later)
   ```bash
   dotnet --version  # Should output 10.x.x
   ```

2. **.NET Aspire workload**
   ```bash
   dotnet workload install aspire
   ```

3. **[Docker Desktop](https://www.docker.com/products/docker-desktop)** (optional — only needed if you add container-based resources like databases or Redis)
   - Not required for the default Aspire Dashboard or local development

4. **A modern web browser**
   - Chrome, Edge, Firefox, or Safari
   - Required for viewing the Blazor frontend and Aspire Dashboard

### Verify Installation

```bash
# Check .NET version
dotnet --version

# Verify Aspire workload
dotnet workload list | findstr aspire

# Verify Docker is running (only if using container-based resources)
docker ps
```

---

## 🚀 Quick Start

### Clone and Run

```bash
# Clone the repository
git clone https://github.com/swigerb/squad-commerce.git
cd squad-commerce

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run with Aspire orchestration
dotnet run --project src\SquadCommerce.AppHost
```

### What Happens on Startup

When you run the application:

1. **Aspire Dashboard opens automatically** (port assigned dynamically)
   - View all running services
   - Monitor traces, metrics, and logs
   - See real-time health status

2. **API service starts** (port assigned dynamically by Aspire)
   - RESTful endpoints for agent orchestration
   - AG-UI streaming endpoint (`/api/agui`)
   - SignalR hub for real-time updates

3. **Blazor Web UI starts** (port assigned dynamically by Aspire)
   - Interactive A2UI components
   - Real-time agent chat panel
   - Manager approval workflow

4. **Background services initialize**:
   - MCP server tools register (`GetInventoryLevels`, `UpdateStorePricing`)
   - Agents register with policy enforcement (Orchestrator, MarketIntel, Inventory, Pricing)
   - Demo data loads (12 stores, 16 SKUs, realistic inventory and pricing across 3 categories)

### Expected Startup Output

> **Note:** Ports are assigned dynamically by Aspire. The URLs below are examples — check your console output or Aspire Dashboard for the actual addresses.

```
info: Aspire.Hosting.DistributedApplication[0]
      Aspire Dashboard is available at https://localhost:<port>
info: Aspire.Hosting.DistributedApplication[0]
      Resource api started (http://localhost:<port>)
info: Aspire.Hosting.DistributedApplication[0]
      Resource webfrontend started (http://localhost:<port>)
```

### URLs to Open

> **Note:** Aspire assigns ports dynamically at startup. The table below shows the services you'll access — find the actual URLs in the Aspire Dashboard or console output.

| Service | How to Find URL | Purpose |
|---------|----------------|---------|
| **Aspire Dashboard** | First URL printed to console | Monitor traces, metrics, logs, and service health |
| **API Service** | Listed as `api` in the Dashboard | REST API and AG-UI streaming endpoint |
| **Blazor Web UI** | Listed as `webfrontend` in the Dashboard | Interactive frontend with A2UI components |
| **Swagger UI** | API URL + `/swagger` | API documentation and testing |

---

## 🎯 Demo Walkthrough: Competitor Price Drop Scenario

### The Business Scenario

> *"Our competitor just dropped the price of the Wireless Mouse (SKU-1001) from $29.99 to $24.99. What should we do?"*

Squad Commerce answers this question automatically by orchestrating four specialized agents through a complete analysis-to-action pipeline.

---

### Step 1: Trigger Analysis

**Trigger the competitor price analysis** by sending a POST request to the `/api/agents/analyze` endpoint.

#### Using cURL (PowerShell)

> **Note:** The port numbers in curl/Invoke-RestMethod examples below (e.g., `7000`) are placeholders. Your actual ports will differ — check the Aspire Dashboard or console output for the real API URL and substitute it in these commands.

```powershell
# Trigger analysis for SKU-1001 (Wireless Mouse) with competitor price drop
$body = @{
    Sku = "SKU-1001"
    CompetitorName = "TechMart"
    CompetitorPrice = 24.99
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7000/api/agents/analyze" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

#### Using cURL (Command Prompt / Bash)

```bash
curl -X POST "https://localhost:7000/api/agents/analyze" \
  -H "Content-Type: application/json" \
  -d "{\"Sku\":\"SKU-1001\",\"CompetitorName\":\"TechMart\",\"CompetitorPrice\":24.99}"
```

#### Expected Response

```json
{
  "sessionId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "message": "Analysis started. Connect to AG-UI stream to receive updates.",
  "streamUrl": "/api/agui?sessionId=a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d"
}
```

#### What Happens Behind the Scenes

1. **ChiefSoftwareArchitect** (Orchestrator) receives the request and creates a session
2. **MarketIntelAgent** validates competitor pricing via A2A protocol
3. **InventoryAgent** queries store inventory using MCP tools (`GetInventoryLevels`)
4. **PricingAgent** calculates margin impact across all 5 stores
5. **AG-UI events** are streamed to connected clients
6. **A2UI components** are generated and sent for rendering in the Blazor UI
7. **OpenTelemetry traces** capture every step for full auditability

---

### Step 2: Watch the AG-UI Stream

**Connect to the Server-Sent Events (SSE) stream** to receive real-time agent updates.

#### Using cURL to Monitor the Stream

```powershell
# Replace SESSION_ID with the value from Step 1
$sessionId = "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d"
curl -N "https://localhost:7000/api/agui?sessionId=$sessionId"
```

#### Event Types You'll See

The AG-UI protocol emits events in SSE format: `data: {json}\n\n`

##### 1. **status_update** — Agent pipeline progress
```json
data: {"type":"status_update","data":{"status":"ChiefSoftwareArchitect orchestrating analysis..."}}

data: {"type":"status_update","data":{"status":"MarketIntelAgent validating competitor pricing via A2A..."}}

data: {"type":"status_update","data":{"status":"InventoryAgent querying store inventory via MCP..."}}

data: {"type":"status_update","data":{"status":"PricingAgent calculating margin impact..."}}
```

##### 2. **tool_call** — MCP tool invocation
```json
data: {"type":"tool_call","data":{"tool":"GetInventoryLevels","parameters":{"sku":"SKU-1001"}}}
```

##### 3. **a2ui_payload** — Generative UI component data
```json
data: {"type":"a2ui_payload","data":{"Type":"RetailStockHeatmap","RenderAs":"RetailStockHeatmap","Data":{"sku":"SKU-1001","productName":"Wireless Mouse","stores":[{"storeId":"SEA-001","storeName":"Downtown Flagship","quantityOnHand":45,"reorderPoint":20,"status":"Good"},{"storeId":"PDX-002","storeName":"Suburban Mall","quantityOnHand":38,"reorderPoint":20,"status":"Good"},{"storeId":"SFO-003","storeName":"Airport Terminal","quantityOnHand":52,"reorderPoint":20,"status":"Good"},{"storeId":"LAX-004","storeName":"University District","quantityOnHand":29,"reorderPoint":20,"status":"Good"},{"storeId":"DEN-005","storeName":"Waterfront Plaza","quantityOnHand":34,"reorderPoint":20,"status":"Good"}]}}}
```

##### 4. **text_delta** — Streaming text response
```json
data: {"type":"text_delta","data":{"text":"Analysis complete for SKU SKU-1001."}}
```

##### 5. **done** — Stream completion marker
```json
data: {"type":"done","data":{"completed":true}}
```

#### AG-UI Stream Timeline

| Time | Event | Agent | Description |
|------|-------|-------|-------------|
| 0ms | `status_update` | Orchestrator | "ChiefSoftwareArchitect orchestrating analysis..." |
| 500ms | `status_update` | MarketIntel | "MarketIntelAgent validating competitor pricing via A2A..." |
| 1300ms | `status_update` | Inventory | "InventoryAgent querying store inventory via MCP..." |
| 1900ms | `tool_call` | Inventory | Calls `GetInventoryLevels` MCP tool |
| 2600ms | `a2ui_payload` | Inventory | Emits `RetailStockHeatmap` component |
| 2700ms | `status_update` | Pricing | "PricingAgent calculating margin impact..." |
| 3400ms | `a2ui_payload` | Pricing | Emits `PricingImpactChart` component |
| 3500ms | `a2ui_payload` | Pricing | Emits `MarketComparisonGrid` component |
| 3600ms | `text_delta` | Orchestrator | "Analysis complete for SKU SKU-1001." |
| 3700ms | `done` | Orchestrator | Stream completed |

---

### Step 3: Review A2UI Components

The agents emit **generative UI components** (A2UI payloads) that render in the Blazor frontend. These are not markdown tables — they're rich, interactive Blazor components.

#### Component 1: RetailStockHeatmap

**Purpose:** Visual inventory levels across all 5 stores

**What it Shows:**
- Store ID and name (e.g., SEA-001 — Downtown Flagship)
- Quantity on hand vs. reorder point
- Color-coded status:
  - 🔴 **Critical** (below reorder point) — red background
  - 🟡 **Low** (1-20% above reorder point) — yellow background
  - 🟢 **Good** (more than 20% above reorder point) — green background
- Percentage bar visualization
- Last restocked timestamp

**Example for SKU-1001 (Wireless Mouse):**

| Store | Name | On Hand | Reorder Point | Status | % of Target |
|-------|------|---------|---------------|--------|-------------|
| SEA-001 | Downtown Flagship | 45 | 20 | 🟢 Good | 225% |
| PDX-002 | Suburban Mall | 38 | 20 | 🟢 Good | 190% |
| SFO-003 | Airport Terminal | 52 | 20 | 🟢 Good | 260% |
| LAX-004 | University District | 29 | 20 | 🟢 Good | 145% |
| DEN-005 | Waterfront Plaza | 34 | 20 | 🟢 Good | 170% |

**Interactive Features:**
- Sortable by store, quantity, or status
- Hover to see last restocked date
- Color-coded for quick visual assessment

---

#### Component 2: PricingImpactChart

**Purpose:** Compare pricing scenarios and margin impact

**What it Shows:**
- **Scenario 1: Keep Current Price** ($29.99)
  - Current margin: 50.0% ($15.00 profit per unit)
  - Estimated revenue impact: Baseline
- **Scenario 2: Match Competitor** ($24.99)
  - New margin: 40.0% ($10.00 profit per unit)
  - Margin delta: -10.0% (-$5.00 per unit)
  - Estimated revenue increase from volume boost
- **Scenario 3: Undercut Competitor** ($23.99)
  - New margin: 37.3% ($9.00 profit per unit)
  - Margin delta: -12.7% (-$6.00 per unit)
  - Maximum competitive advantage

**Visual Elements:**
- Price flow diagram: `$29.99 → $24.99`
- Margin percentage cards with delta indicators (▼ -10.0%)
- Color-coded profit impact (red for decrease, green for increase)
- Clickable scenarios for selection

**Interactive Features:**
- Click a scenario to select it for approval
- Hover to see detailed calculations
- Visual comparison of margin trade-offs

---

#### Component 3: MarketComparisonGrid

**Purpose:** Competitive landscape analysis

**What it Shows:**

| Competitor | Price | Delta vs. Us | Position | Verified |
|------------|-------|--------------|----------|----------|
| **Us (Squad Commerce)** | **$29.99** | **Baseline** | **#3** | ✅ Current |
| TechMart | $24.99 | -$5.00 (16.7% lower) | #1 | ✅ A2A Verified |
| GadgetHub | $26.49 | -$3.50 (11.7% lower) | #2 | ✅ A2A Verified |
| ElectroWorld | $31.99 | +$2.00 (6.7% higher) | #4 | ✅ A2A Verified |
| BudgetBytes | $27.99 | -$2.00 (6.7% lower) | #2 | ⚠️ Unverified |

**Visual Elements:**
- Trend indicators (▼ lower, ▲ higher)
- Verification badges (✅ A2A Verified, ⚠️ Unverified)
- Market position summary cards
- Color-coded price deltas

**Interactive Features:**
- Sortable by competitor, price, or delta
- Click competitor to see historical pricing trends
- Filter by verification status

---

### Step 4: Manager Decision

After reviewing the A2UI components, the manager can make a decision: **Approve**, **Modify**, or **Reject**.

#### Option 1: Approve Pricing Proposal

**Action:** Apply the proposed price changes to all stores.

```powershell
# Approve the proposal (match competitor at $24.99)
$approvalBody = @{
    ProposalId = "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d"
    ApprovedBy = "jane.manager@squadcommerce.com"
    StoreIds = @("SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005")
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7000/api/pricing/approve" `
    -Method POST `
    -ContentType "application/json" `
    -Body $approvalBody
```

**Expected Response:**
```json
{
  "proposalId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "action": "Approved",
  "success": true,
  "message": "Pricing updates applied to 5 store(s). PricingAgent executed UpdateStorePricing MCP tool.",
  "updatedStores": ["SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005"],
  "timestamp": "2026-03-24T19:45:32.123Z"
}
```

**What Happens:**
1. PricingAgent invokes `UpdateStorePricing` MCP tool for each store
2. Prices update atomically in the pricing repository
3. Audit logs record the change with manager approval
4. OpenTelemetry traces capture the full workflow
5. Metrics record the pricing decision: `pricing_decision_total{action="approved"}`

---

#### Option 2: Reject Pricing Proposal

**Action:** Reject the proposal with a reason.

```powershell
# Reject the proposal
$rejectionBody = @{
    ProposalId = "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d"
    RejectedBy = "jane.manager@squadcommerce.com"
    Reason = "Margin impact too high. Need to evaluate supply chain cost reductions first."
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7000/api/pricing/reject" `
    -Method POST `
    -ContentType "application/json" `
    -Body $rejectionBody
```

**Expected Response:**
```json
{
  "proposalId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "action": "Rejected",
  "success": true,
  "message": "Proposal rejected. Reason: Margin impact too high. Need to evaluate supply chain cost reductions first.",
  "updatedStores": [],
  "timestamp": "2026-03-24T19:47:15.456Z"
}
```

**What Happens:**
1. Rejection is logged with reason
2. No pricing changes are applied
3. Audit trail captures the rejection
4. Metrics record: `pricing_decision_total{action="rejected"}`

---

#### Option 3: Modify Pricing Proposal

**Action:** Adjust the proposed prices and re-trigger calculation.

```powershell
# Modify prices (use $25.99 instead of $24.99)
$modificationBody = @{
    ProposalId = "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d"
    ModifiedBy = "jane.manager@squadcommerce.com"
    ModifiedPrices = @(
        @{ Sku = "SKU-1001"; StoreId = "SEA-001"; NewPrice = 25.99 },
        @{ Sku = "SKU-1001"; StoreId = "PDX-002"; NewPrice = 25.99 },
        @{ Sku = "SKU-1001"; StoreId = "SFO-003"; NewPrice = 25.99 },
        @{ Sku = "SKU-1001"; StoreId = "LAX-004"; NewPrice = 25.99 },
        @{ Sku = "SKU-1001"; StoreId = "DEN-005"; NewPrice = 25.99 }
    )
} | ConvertTo-Json -Depth 3

Invoke-RestMethod -Uri "https://localhost:7000/api/pricing/modify" `
    -Method POST `
    -ContentType "application/json" `
    -Body $modificationBody
```

**Expected Response:**
```json
{
  "proposalId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "action": "Modified",
  "success": true,
  "message": "Modified prices received. Re-triggering PricingAgent calculation with new values.",
  "updatedStores": ["SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005"],
  "timestamp": "2026-03-24T19:49:08.789Z"
}
```

**What Happens:**
1. PricingAgent recalculates margin impact with new prices
2. New A2UI components are generated with updated scenarios
3. Manager reviews the revised proposal
4. Metrics record: `pricing_decision_total{action="modified"}`

---

### Step 5: Verify in Aspire Dashboard

The **Aspire Dashboard** provides full observability into the agent orchestration pipeline.

**Open the Dashboard:** Check console output for the Aspire Dashboard URL (assigned dynamically at startup).

#### View 1: Traces — Full Orchestration Hierarchy

Navigate to **Traces** and search for your session ID or trace ID.

**Trace Hierarchy:**
```
▼ ChiefSoftwareArchitect.Orchestrate (2,700ms)
  ├─ MarketIntelAgent.Execute (800ms)
  │  └─ A2A.ValidateCompetitorPrice (650ms)
  ├─ InventoryAgent.Execute (600ms)
  │  └─ MCP.GetInventoryLevels (450ms)
  └─ PricingAgent.Execute (700ms)
     ├─ MCP.GetInventoryLevels (200ms)
     ├─ Pricing.CalculateMarginImpact (350ms)
     └─ AGUI.EmitA2UIPayload (50ms)
```

**What Each Span Shows:**
- **Duration:** Total time for each agent and tool call
- **Tags:** `session.id`, `sku`, `agent.name`, `tool.name`
- **Events:** Internal reasoning steps, decisions, and errors
- **Status:** Success (green) or failure (red)

**How to Use Traces:**
- Click any span to see detailed logs and tags
- Find bottlenecks (which agent took the longest?)
- Debug failures (which step failed and why?)
- Audit decisions (what data did each agent see?)

---

#### View 2: Metrics — Custom Squad Commerce Metrics

Navigate to **Metrics** and view custom metrics emitted by the application.

**Available Metrics:**

| Metric Name | Type | Description | Labels |
|-------------|------|-------------|--------|
| `agent_invocation_duration_ms` | Histogram | Agent execution time | `agent_name`, `success` |
| `agent_invocation_total` | Counter | Total agent invocations | `agent_name`, `success` |
| `mcp_tool_duration_ms` | Histogram | MCP tool execution time | `tool_name`, `success` |
| `mcp_tool_total` | Counter | Total MCP tool calls | `tool_name`, `success` |
| `a2ui_payload_total` | Counter | A2UI components emitted | `component_type`, `session_id` |
| `agui_stream_duration_ms` | Histogram | AG-UI stream connection time | `session_id` |
| `pricing_decision_total` | Counter | Pricing decisions made | `action` (approved/rejected/modified) |
| `a2a_handshake_total` | Counter | A2A protocol handshakes | `partner`, `success` |

**Example Queries:**
- **Average agent execution time:** `avg(agent_invocation_duration_ms) by agent_name`
- **MCP tool success rate:** `sum(mcp_tool_total{success="true"}) / sum(mcp_tool_total)`
- **A2UI component breakdown:** `sum(a2ui_payload_total) by component_type`
- **Pricing decision distribution:** `sum(pricing_decision_total) by action`

---

#### View 3: Structured Logs — Agent Reasoning

Navigate to **Logs** and filter by your session ID.

**Log Levels:**
- **Information:** Agent pipeline progress, tool calls, decisions
- **Debug:** Internal reasoning steps, data transformations
- **Warning:** Potential issues (low inventory, margin below threshold)
- **Error:** Failures, validation errors, retries

**Example Log Entries:**

```
[INFO] ChiefSoftwareArchitect orchestrating analysis: SessionId=a7b3c5d9, Sku=SKU-1001, TraceId=4f3e2d1c
[INFO] MarketIntelAgent executing: SessionId=a7b3c5d9, Sku=SKU-1001
[DEBUG] A2A handshake initiated with partner TechMart
[INFO] InventoryAgent executing: SessionId=a7b3c5d9, Sku=SKU-1001
[DEBUG] MCP tool GetInventoryLevels called: Sku=SKU-1001
[INFO] PricingAgent executing: SessionId=a7b3c5d9, Sku=SKU-1001
[DEBUG] Margin impact calculated: Current=50.0%, Proposed=40.0%, Delta=-10.0%
[INFO] A2UI payload emitted: SessionId=a7b3c5d9, ComponentType=RetailStockHeatmap
[INFO] A2UI payload emitted: SessionId=a7b3c5d9, ComponentType=PricingImpactChart
[INFO] Analysis workflow completed: SessionId=a7b3c5d9, Duration=2700ms
```

**How to Use Logs:**
- Filter by `SessionId` to see all logs for a specific analysis
- Filter by `AgentName` to see what a specific agent did
- Search for errors: `Level=Error`
- Correlate logs with traces using `TraceId`

---

## 🎯 Demo Walkthrough: Scenario 2 — Viral Spike

### Business Context

A social media post about your product goes viral. You have hours to respond before the trend peaks. Traditional retail organizations take days to react to viral moments — by then the trend has passed. Squad Commerce detects the spike, adjusts pricing on complementary items, and generates marketing assets in real time.

### Demo Prompt

Type this into the Squad Commerce chat UI:

```
A TikTok influencer just posted about our Classic Denim line. Demand is spiking 400% in the Northeast. Can we capitalize on this without stockouts?
```

### What Happens

1. **ChiefSoftwareArchitect** (Orchestrator) detects viral/social keywords and routes to the viral spike pipeline
2. **MarketIntelAgent** analyzes social sentiment velocity across platforms (TikTok, Instagram, Twitter) via A2A
3. **PricingAgent** calculates flash sale scenarios on complementary items to maximize basket size
4. **MarketingAgent** generates ready-to-deploy campaign assets (email templates, hero banners)
5. **AG-UI events** stream real-time progress to the Blazor frontend
6. **A2UI components** render interactive sentiment graphs, campaign previews, and pricing scenarios

### What You'll See

| A2UI Component | Description |
|----------------|-------------|
| **SocialSentimentGraph** | Real-time sentiment velocity by platform — shows TikTok spike, Instagram echo, Twitter trailing. Includes trend line and peak prediction. |
| **CampaignPreview** | Generated email template + hero banner mockup. Shows subject line, CTA, and discount code. Editable before deployment. |
| **PricingImpactChart** | Flash sale scenarios on complementary items (e.g., accessories for the Classic Denim line). Shows margin trade-offs for 10%, 15%, and 20% discounts. |

### Business Outcome

**Dynamic response to viral moments** — flash sale pricing on complementary products + ready-to-deploy marketing assets, all generated in under 60 seconds. Captures revenue during the trend window instead of missing it entirely.

---

## 🎯 Demo Walkthrough: Scenario 3 — Supply Chain Shock

### Business Context

A weather event disrupts your supply chain. Stock is running low at key stores. Manual redistribution takes days of phone calls between store managers. Squad Commerce identifies affected stores, calculates optimal store-to-store transfers, and presents a redistribution plan — all in seconds.

### Demo Prompt

Type this into the Squad Commerce chat UI:

```
Our main shipment of SKU-2001 Organic Coffee is delayed by 3 days due to a storm. How do we minimize the impact on our top regional stores?
```

### What Happens

1. **ChiefSoftwareArchitect** (Orchestrator) detects supply chain disruption keywords and routes to the redistribution pipeline
2. **LogisticsAgent** verifies the shipment delay and estimates the impact window
3. **InventoryAgent** queries current stock levels across all 12 stores using MCP tools (`GetInventoryLevels`)
4. **RedistributionAgent** plans optimal store-to-store transfers to cover high-risk locations
5. **AG-UI events** stream transfer calculations in real time
6. **A2UI components** render an interactive rerouting map and risk assessment

### What You'll See

| A2UI Component | Description |
|----------------|-------------|
| **ReroutingMap** | Store network visualization with transfer arrows showing source → destination stores. Includes transfer quantities and estimated delivery times. |
| **RetailStockHeatmap** | Current inventory levels across all affected stores, color-coded by risk. Highlights stores below reorder point in red. |
| **RiskScoreGauge** | Overall stockout risk score (0-100) before and after redistribution. Shows percentage reduction in customer-facing stockout probability. |

### Business Outcome

**Automated redistribution reduces customer-facing stockouts by 60–80%.** Instead of days of manual coordination, the system produces an optimized transfer plan in seconds — preserving revenue at high-traffic locations while minimizing logistics cost.

---

## 🎯 Demo Walkthrough: Scenario 4 — Store Readiness

### Business Context

A new flagship store opens in days. The layout needs to be optimized for local shopping patterns, but traditional planogram design relies on gut instinct. Squad Commerce uses foot traffic heatmaps and sales data to generate a data-driven layout — with manager sign-off before anything moves.

### Demo Prompt

Type this into the Squad Commerce chat UI:

```
We are opening the new Miami Flagship store on Friday. The Electronics section layout isn't optimized for current foot traffic trends. Fix it.
```

### What Happens

1. **ChiefSoftwareArchitect** (Orchestrator) detects store readiness keywords and routes to the layout optimization pipeline
2. **TrafficAnalystAgent** pulls foot traffic heatmap data for the store region and comparable locations
3. **MerchandisingAgent** generates an optimized planogram based on traffic flow, product adjacency, and margin contribution
4. **ManagerAgent** requests Human-in-the-Loop (HITL) approval before finalizing the layout
5. **AG-UI events** stream the analysis and layout generation in real time
6. **A2UI components** render an interactive floorplan with approval controls

### What You'll See

| A2UI Component | Description |
|----------------|-------------|
| **InteractiveFloorplan** | Full store layout with color-coded traffic heatmap overlay. Individual sections (Electronics, Grocery, Apparel) are clickable. Shelf-level approve/reject buttons let the manager sign off on each zone. HITL workflow — nothing changes without explicit approval. |

### Business Outcome

**Data-driven store layouts with manager sign-off before opening day.** Replaces weeks of manual planogram design with AI-generated layouts grounded in real traffic data. The HITL approval ensures human judgment remains in the loop for high-stakes decisions.

---

## 🎯 Demo Walkthrough: Scenario 5 — ESG Audit

### Business Context

New regulations require certification verification for all suppliers in a product category. Non-compliance means fines, shipment holds, and reputational damage. Manually auditing supplier certifications across hundreds of SKUs takes weeks. Squad Commerce automates the entire audit in minutes.

### Demo Prompt

Type this into the Squad Commerce chat UI:

```
A new regulation requires us to verify the Fair Trade certification of all suppliers for our Cocoa-based SKUs by next week. Who is at risk?
```

### What Happens

1. **ChiefSoftwareArchitect** (Orchestrator) detects compliance/ESG keywords and routes to the audit pipeline
2. **ComplianceAgent** checks certification records for all suppliers associated with Cocoa-based SKUs
3. **ResearchAgent** cross-references suppliers against regulatory watchlists and industry databases via A2A
4. **ProcurementAgent** identifies pre-vetted alternative suppliers for any flagged vendors
5. **AG-UI events** stream the audit progress in real time
6. **A2UI components** render a comprehensive supplier risk matrix

### What You'll See

| A2UI Component | Description |
|----------------|-------------|
| **SupplierRiskMatrix** | Supplier cards showing: supplier name, certification status (✅ Valid, ⚠️ Expiring, ❌ Missing), risk level (Low/Medium/High/Critical), deadline countdown, and pre-vetted alternative suppliers. Sortable by risk level. Actionable — click to initiate procurement switch. |

### Business Outcome

**Proactive compliance — identify and replace at-risk suppliers before the deadline.** Transforms a weeks-long manual audit into a minutes-long automated process. Prevents regulatory fines and protects brand reputation by ensuring full supply chain certification coverage.

---

## 📚 API Reference

### Agent Endpoints

#### GET `/api/agents`
**List all registered agents and their policies**

**Response:**
```json
{
  "agents": [
    {
      "name": "ChiefSoftwareArchitect",
      "role": "Orchestrator",
      "entraIdScope": "SquadCommerce.Orchestrate",
      "allowedTools": [],
      "preferredProtocol": "AGUI"
    },
    {
      "name": "InventoryAgent",
      "role": "Domain",
      "entraIdScope": "SquadCommerce.Inventory.Read",
      "allowedTools": ["GetInventoryLevels"],
      "preferredProtocol": "MCP"
    },
    {
      "name": "PricingAgent",
      "role": "Domain",
      "entraIdScope": "SquadCommerce.Pricing.ReadWrite",
      "allowedTools": ["GetInventoryLevels", "UpdateStorePricing"],
      "preferredProtocol": "MCP"
    },
    {
      "name": "MarketIntelAgent",
      "role": "Domain",
      "entraIdScope": "SquadCommerce.MarketIntel.Read",
      "allowedTools": [],
      "preferredProtocol": "A2A"
    }
  ]
}
```

---

#### GET `/api/agents/{name}/status`
**Get status of a specific agent**

**Parameters:**
- `name` (path) — Agent name (e.g., `InventoryAgent`)

**Response:**
```json
{
  "agentName": "InventoryAgent",
  "status": "Idle",
  "lastActivity": "2026-03-24T19:30:15.123Z",
  "activeSessions": 0
}
```

---

#### POST `/api/agents/analyze`
**Trigger competitor price drop analysis**

**Request Body:**
```json
{
  "sku": "SKU-1001",
  "competitorName": "TechMart",
  "competitorPrice": 24.99
}
```

**Response:**
```json
{
  "sessionId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "message": "Analysis started. Connect to AG-UI stream to receive updates.",
  "streamUrl": "/api/agui?sessionId=a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d"
}
```

---

### Pricing Endpoints

#### POST `/api/pricing/approve`
**Approve pricing proposal and execute updates**

**Request Body:**
```json
{
  "proposalId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "approvedBy": "jane.manager@squadcommerce.com",
  "storeIds": ["SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005"]
}
```

**Response:**
```json
{
  "proposalId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "action": "Approved",
  "success": true,
  "message": "Pricing updates applied to 5 store(s). PricingAgent executed UpdateStorePricing MCP tool.",
  "updatedStores": ["SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005"],
  "timestamp": "2026-03-24T19:45:32.123Z"
}
```

---

#### POST `/api/pricing/reject`
**Reject pricing proposal with reason**

**Request Body:**
```json
{
  "proposalId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "rejectedBy": "jane.manager@squadcommerce.com",
  "reason": "Margin impact too high. Need to evaluate supply chain cost reductions first."
}
```

**Response:**
```json
{
  "proposalId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "action": "Rejected",
  "success": true,
  "message": "Proposal rejected. Reason: Margin impact too high...",
  "updatedStores": [],
  "timestamp": "2026-03-24T19:47:15.456Z"
}
```

---

#### POST `/api/pricing/modify`
**Modify proposed prices and re-trigger calculation**

**Request Body:**
```json
{
  "proposalId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "modifiedBy": "jane.manager@squadcommerce.com",
  "modifiedPrices": [
    { "sku": "SKU-1001", "storeId": "SEA-001", "newPrice": 25.99 },
    { "sku": "SKU-1001", "storeId": "PDX-002", "newPrice": 25.99 }
  ]
}
```

**Response:**
```json
{
  "proposalId": "a7b3c5d9-4f2e-4a1b-9c8d-7e6f5a4b3c2d",
  "action": "Modified",
  "success": true,
  "message": "Modified prices received. Re-triggering PricingAgent calculation with new values.",
  "updatedStores": ["SEA-001", "PDX-002"],
  "timestamp": "2026-03-24T19:49:08.789Z"
}
```

---

### AG-UI Streaming Endpoint

#### GET `/api/agui?sessionId={sessionId}`
**Server-Sent Events stream for agent communication**

**Parameters:**
- `sessionId` (query) — Session ID from `/api/agents/analyze` response

**Response Format:** `text/event-stream`

**Event Types:**
- `status_update` — Pipeline progress
- `tool_call` — MCP tool invocation
- `a2ui_payload` — Generative UI component
- `text_delta` — Streaming text
- `done` — Stream completion

**Example:**
```
data: {"type":"status_update","data":{"status":"InventoryAgent querying store inventory via MCP..."}}

data: {"type":"a2ui_payload","data":{"Type":"RetailStockHeatmap","RenderAs":"RetailStockHeatmap","Data":{...}}}

data: {"type":"done","data":{"completed":true}}
```

---

### Scenario Detection Keywords

The orchestrator detects the scenario type from natural language input. Use these keywords to trigger each pipeline:

| Scenario | Trigger Keywords | Example Phrases |
|----------|-----------------|-----------------|
| **1. Competitor Price Drop** | SKU identifier + price (e.g., `SKU-1001`, `$24.99`) | "Competitor dropped SKU-1001 to $24.99" |
| **2. Viral Spike** | `viral`, `TikTok`, `trending`, `spike`, `influencer` | "A TikTok influencer posted about our product" |
| **3. Supply Chain Shock** | `delayed`, `shipment`, `storm`, `supply chain` | "Our shipment is delayed by a storm" |
| **4. Store Readiness** | `new store`, `opening`, `flagship`, `layout` | "We are opening a new flagship store on Friday" |
| **5. ESG Audit** | `certification`, `Fair Trade`, `ESG`, `compliance` | "Verify Fair Trade certification for all suppliers" |

> **Note:** The orchestrator uses keyword matching combined with intent classification. Including at least one keyword from the table above ensures reliable routing to the correct agent pipeline.

---

## 🏗️ Architecture Overview

### System Architecture Diagram

![Squad Commerce Agent Architecture](squad-commerce-architecture.png)

### Project Structure

| Project | Purpose | Key Technologies |
|---------|---------|------------------|
| **SquadCommerce.AppHost** | .NET Aspire orchestrator | Aspire, Docker, OpenTelemetry |
| **SquadCommerce.ServiceDefaults** | Shared service configuration | OpenTelemetry, Health Checks, Metrics |
| **SquadCommerce.Api** | REST API and AG-UI streaming | ASP.NET Core, SignalR, SSE |
| **SquadCommerce.Web** | Blazor frontend with A2UI | Blazor Server, A2UI components, SignalR client |
| **SquadCommerce.Agents** | MAF agent definitions | Microsoft Agent Framework, Graph Workflows |
| **SquadCommerce.Mcp** | MCP server tools | Model Context Protocol, ERP/SQL integration |
| **SquadCommerce.A2A** | A2A protocol handlers | Agent-to-Agent protocol, external partners |
| **SquadCommerce.Contracts** | Shared models and interfaces | DTOs, interfaces, enums |
| **SquadCommerce.Observability** | Telemetry and metrics | OpenTelemetry, custom metrics |

### Agent Flow

```
1. User Request → API → ChiefSoftwareArchitect (Orchestrator)
2. Orchestrator → MarketIntelAgent (A2A validation)
3. Orchestrator → InventoryAgent (MCP tools: GetInventoryLevels)
4. Orchestrator → PricingAgent (MCP tools: GetInventoryLevels, UpdateStorePricing)
5. PricingAgent → AG-UI Stream (A2UI payloads: RetailStockHeatmap, PricingImpactChart, MarketComparisonGrid)
6. Blazor UI → Renders A2UI components
7. Manager → Decision (Approve/Reject/Modify via API)
8. PricingAgent → MCP tool UpdateStorePricing
9. Audit logs + OpenTelemetry traces capture everything
```

### Protocol Usage

| Protocol | Used By | Purpose |
|----------|---------|---------|
| **MAF** | All agents | Orchestration, graph-based workflows, reasoning |
| **MCP** | InventoryAgent, PricingAgent | Tool execution (GetInventoryLevels, UpdateStorePricing) |
| **A2A** | MarketIntelAgent | External vendor communication, competitor data validation |
| **AG-UI** | All agents → Web UI | Streaming responses, status updates, generative UI |
| **A2UI** | PricingAgent → Blazor | Rich interactive components (heatmaps, charts, grids) |

---

## 📊 Demo Data

### Stores (12 locations)

| Store ID | Name | Region | Type |
|----------|------|--------|------|
| **SEA-001** | Downtown Flagship | Northwest | Urban flagship store |
| **PDX-002** | Suburban Mall | Northwest | Suburban location |
| **SFO-003** | Airport Terminal | West | High-traffic airport location |
| **LAX-004** | University District | West | College campus area |
| **DEN-005** | Waterfront Plaza | Mountain | Waterfront retail complex |
| **CHI-006** | Magnificent Mile | Midwest | Premium urban location |
| **NYC-007** | Times Square Express | Northeast | High-traffic tourist area |
| **BOS-008** | Harvard Square | Northeast | University area |
| **ATL-009** | Peachtree Center | Southeast | Downtown business district |
| **MIA-010** | Miami Flagship | Southeast | Flagship beach district |
| **DAL-011** | Galleria Outlet | South | Outlet mall location |
| **PHX-012** | Desert Ridge | Southwest | Suburban power center |

### Products (16 SKUs)

| SKU | Product Name | Category | Avg. Price | Avg. Cost | Avg. Margin |
|-----|--------------|----------|------------|-----------|-------------|
| **SKU-1001** | Wireless Mouse | Electronics | $29.99 | $15.00 | 50.0% |
| **SKU-1002** | USB-C Cable 6ft | Electronics | $12.99 | $4.50 | 65.4% |
| **SKU-1003** | Laptop Stand | Electronics | $49.99 | $25.00 | 50.0% |
| **SKU-1004** | Webcam 1080p | Electronics | $79.99 | $40.00 | 50.0% |
| **SKU-1005** | Mechanical Keyboard | Electronics | $119.99 | $65.00 | 45.8% |
| **SKU-1006** | Noise-Cancelling Headphones | Electronics | $199.99 | $110.00 | 45.0% |
| **SKU-1007** | External SSD 1TB | Electronics | $89.99 | $50.00 | 44.4% |
| **SKU-1008** | Monitor 27-inch | Electronics | $349.99 | $200.00 | 42.9% |
| **SKU-2001** | Organic Coffee Beans 1lb | Grocery | $14.99 | $8.00 | 46.6% |
| **SKU-2002** | Fair Trade Cocoa Powder | Grocery | $9.99 | $5.50 | 44.9% |
| **SKU-2003** | Artisan Granola Mix | Grocery | $7.49 | $3.50 | 53.3% |
| **SKU-2004** | Cold-Pressed Olive Oil | Grocery | $18.99 | $10.00 | 47.3% |
| **SKU-3001** | Classic Denim Jacket | Apparel | $89.99 | $38.00 | 57.8% |
| **SKU-3002** | Classic Denim Jeans | Apparel | $69.99 | $28.00 | 60.0% |
| **SKU-3003** | Organic Cotton T-Shirt | Apparel | $34.99 | $12.00 | 65.7% |
| **SKU-3004** | Recycled Polyester Hoodie | Apparel | $59.99 | $25.00 | 58.3% |

> **3 Product Categories:** Electronics (8 SKUs), Grocery (4 SKUs), Apparel (4 SKUs)

### Sample Inventory Levels (SKU-1001 — Wireless Mouse)

| Store | Name | On Hand | Reorder Point | Status |
|-------|------|---------|---------------|--------|
| SEA-001 | Downtown Flagship | 45 | 20 | 🟢 Good |
| PDX-002 | Suburban Mall | 38 | 20 | 🟢 Good |
| SFO-003 | Airport Terminal | 52 | 20 | 🟢 Good |
| LAX-004 | University District | 29 | 20 | 🟢 Good |
| DEN-005 | Waterfront Plaza | 34 | 20 | 🟢 Good |

### Sample Pricing (SKU-1001 — Wireless Mouse)

| Store | Price | Cost | Margin % |
|-------|-------|------|----------|
| SEA-001 | $29.99 | $15.00 | 50.0% |
| PDX-002 | $27.99 | $15.00 | 46.4% |
| SFO-003 | $32.99 | $15.00 | 54.6% |
| LAX-004 | $30.99 | $15.00 | 51.6% |
| DEN-005 | $28.99 | $15.00 | 48.3% |

### How Demo Data Works

- **In-Memory Repositories:** `InventoryRepository` and `PricingRepository` in `SquadCommerce.Mcp`
- **Thread-Safe:** Uses `ConcurrentDictionary` for atomic updates
- **Realistic Values:** Prices vary by store, inventory reflects typical turnover patterns
- **192 Records Total:** 12 stores × 16 SKUs = 192 inventory records, 192 pricing records
- **3 Categories:** Electronics (8 SKUs), Grocery (4 SKUs), Apparel (4 SKUs)

### Modifying Demo Data

To add or modify demo data:

1. **Edit `InventoryRepository.cs`:**
   - Add stores to `StoreNames` dictionary
   - Add products to `ProductNames` dictionary
   - Add inventory records to the constructor's data list

2. **Edit `PricingRepository.cs`:**
   - Add pricing records to the constructor's data list
   - Ensure cost and margin are realistic (margin = (price - cost) / price × 100)

3. **Rebuild and restart:**
   ```bash
   dotnet build
   dotnet run --project src\SquadCommerce.AppHost
   ```

---

## 🔧 Troubleshooting

### Issue: Agents Not Registered

**Symptom:** `/api/agents` returns empty list or agents are missing

**Solution:**
1. Verify all agent projects are built:
   ```bash
   dotnet build src\SquadCommerce.Agents
   ```
2. Check agent registration in `Program.cs`:
   ```bash
   grep -r "AddSquadCommerceAgents" src\SquadCommerce.Api\Program.cs
   ```
3. Review startup logs for registration errors:
   ```
   info: SquadCommerce.Agents[0] 4 agents registered with policy enforcement
   ```

---

### Issue: SignalR Connection Failed

**Symptom:** Blazor UI shows "Disconnected" status, no real-time updates

**Solution:**
1. Verify SignalR hub is mapped:
   ```powershell
   curl https://localhost:7000/hubs/agent
   # Should return 405 Method Not Allowed (hub is active but GET not supported)
   ```
2. Check CORS configuration in `Program.cs`:
   - Ensure Blazor origin is allowed: `https://localhost:7001`
   - Verify `.AllowCredentials()` is enabled (required for SignalR)
3. Test SignalR connection from browser console:
   ```javascript
   const connection = new signalR.HubConnectionBuilder()
     .withUrl("https://localhost:7000/hubs/agent")
     .build();
   await connection.start();
   console.log("Connected!");
   ```

---

### Issue: AG-UI Stream Not Receiving Events

**Symptom:** SSE connection opens but no events are received

**Solution:**
1. Verify session ID is correct (from `/api/agents/analyze` response)
2. Check that the analysis is running:
   ```powershell
   Invoke-RestMethod "https://localhost:7000/api/agents/ChiefSoftwareArchitect/status"
   ```
3. Review API logs for stream errors:
   ```bash
   grep "AG-UI" src\SquadCommerce.Api\bin\Debug\net10.0\logs\*
   ```
4. Verify `AgUiStreamWriter` is registered as singleton:
   ```bash
   grep "AddSingleton<IAgUiStreamWriter>" src\SquadCommerce.Api\Program.cs
   ```

---

### Issue: MCP Tools Not Found

**Symptom:** Agent logs show "Tool not found: GetInventoryLevels"

**Solution:**
1. Verify MCP infrastructure is registered:
   ```bash
   grep "AddSquadCommerceMcp" src\SquadCommerce.Api\Program.cs
   ```
2. Check MCP tool registration logs:
   ```
   info: SquadCommerce.Mcp[0] 2 MCP tools registered: GetInventoryLevels, UpdateStorePricing
   ```
3. Ensure repositories are registered as singletons:
   ```bash
   grep "AddSingleton.*Repository" src\SquadCommerce.Mcp\ServiceCollectionExtensions.cs
   ```

---

### Issue: Aspire Dashboard Not Opening

**Symptom:** Dashboard URL doesn't open automatically or shows 404

**Solution:**
1. Verify Aspire workload is installed:
   ```bash
   dotnet workload list | findstr aspire
   ```
2. Manually find the dashboard URL:
   - Look for the URL in startup logs: `Aspire Dashboard is available at https://localhost:<port>`
   - Navigate to that URL in your browser
3. Docker is **not** required for the Aspire Dashboard — it runs as a standalone .NET process
4. If the auto-assigned port is in use, Aspire will assign a different one (check logs for actual URL)

---

### Issue: A2UI Components Not Rendering

**Symptom:** Blazor UI shows empty panels or JSON instead of components

**Solution:**
1. Verify A2UI component namespaces are imported:
   ```bash
   grep "A2UI" src\SquadCommerce.Web\Components\_Imports.razor
   ```
2. Check component registration in `A2UIRenderer.razor`:
   - `RetailStockHeatmap`
   - `PricingImpactChart`
   - `MarketComparisonGrid`
3. Review browser console for JavaScript errors:
   - Press F12 → Console tab
   - Look for `Blazor` or `SignalR` errors
4. Verify A2UI payloads are emitted correctly:
   ```bash
   grep "A2UI payload emitted" src\SquadCommerce.Api\bin\Debug\net10.0\logs\*
   ```

---

### Issue: Pricing Update Below Cost

**Symptom:** `/api/pricing/approve` fails with "New price is below cost"

**Solution:**
1. Check product cost in `PricingRepository.cs`:
   - SKU-1001 cost is $15.00
   - Proposed price must be > $15.00
2. Verify competitor price in analysis request:
   ```json
   { "competitorPrice": 24.99 }  ✅ Valid (> $15.00)
   { "competitorPrice": 12.99 }  ❌ Invalid (< $15.00)
   ```
3. If cost is too high, adjust in `PricingRepository.cs` and rebuild

---

### Issue: Slow Agent Execution

**Symptom:** Analysis takes longer than expected (> 5 seconds)

**Solution:**
1. Check Aspire Dashboard → Traces for bottlenecks:
   - Which agent span took the longest?
   - Are there retries or timeouts?
2. Review custom metrics in Dashboard → Metrics:
   - `agent_invocation_duration_ms` — Agent execution time
   - `mcp_tool_duration_ms` — Tool call overhead
3. Verify simulated delays in `AgentEndpoints.cs` (demo only):
   - MarketIntel: 800ms
   - Inventory: 600ms
   - Pricing: 700ms
4. For production, remove `Task.Delay` calls and use real agent implementations

---

### Getting Help

If you encounter issues not covered here:

1. **Check Aspire Dashboard logs:**
   - Navigate to `https://localhost:15888` → Logs
   - Filter by service: `SquadCommerce.Api`, `SquadCommerce.Web`, etc.

2. **Enable debug logging:**
   ```json
   // appsettings.Development.json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "SquadCommerce": "Debug"
       }
     }
   }
   ```

3. **Run integration tests:**
   ```bash
   dotnet test tests\SquadCommerce.Integration.Tests
   ```

4. **Open an issue:**
   - GitHub: [https://github.com/swigerb/squad-commerce/issues](https://github.com/swigerb/squad-commerce/issues)
   - Include: Logs, trace IDs, error messages, steps to reproduce

---

## 🎓 What You've Learned

By completing this demo, you've experienced:

✅ **5 Enterprise Scenarios** — Competitor pricing, viral response, supply chain resilience, store optimization, and ESG compliance  
✅ **Microsoft Agent Framework (MAF)** — Graph-based workflows orchestrating multiple specialized agents  
✅ **Model Context Protocol (MCP)** — Tool execution for inventory and pricing operations  
✅ **Agent-to-Agent (A2A)** — External vendor communication for competitor validation  
✅ **Agent-to-UI (AG-UI)** — Real-time streaming responses via Server-Sent Events  
✅ **A2UI Generative UI** — Rich Blazor components rendered from agent payloads (heatmaps, floorplans, risk matrices)  
✅ **Human-in-the-Loop (HITL)** — Manager approval workflows for high-stakes decisions  
✅ **OpenTelemetry** — Distributed tracing for full observability  
✅ **.NET Aspire** — Modern orchestration with built-in dashboard and telemetry  
✅ **Enterprise Patterns** — Audit logs, policy enforcement, manager approval workflows

---

## 📄 Next Steps

- **Try All 5 Scenarios:** Run through each demo scenario to see the full range of agent capabilities
- **Extend the Scenarios:** Add more SKUs, stores, or competitor sources
- **Customize Agents:** Modify agent prompts and policies in `SquadCommerce.Agents`
- **Build New A2UI Components:** Create custom Blazor components in `SquadCommerce.Web`
- **Deploy to Production:** Use Aspire for container orchestration and Azure deployment
- **Integrate Real Data:** Replace in-memory repositories with SQL Server, Cosmos DB, or SAP

---

<p align="center">
  <em>Built with ❤️ by humans and AI agents, orchestrated by Squad</em>
</p>

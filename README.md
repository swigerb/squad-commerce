# 🛒 Squad Commerce

> **A Microsoft showcase of excellence in AI-powered retail** — demonstrating the best of Microsoft Agent Framework, A2A, MCP, AG-UI, and A2UI in an enterprise commerce application.

[![Built with MAF](https://img.shields.io/badge/Built%20with-Microsoft%20Agent%20Framework-blue)](#technology-stack)
[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](#technology-stack)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![CI](https://github.com/swigerb/squad-commerce/actions/workflows/ci.yml/badge.svg)](https://github.com/swigerb/squad-commerce/actions/workflows/ci.yml)

---

## 🎯 What Is This?

Squad Commerce is a **pro-code, enterprise-grade multi-agent system** that solves retail supply chain silos for consumer goods organizations. It demonstrates how AI agents can work together — coordinating across inventory management, pricing optimization, and market intelligence — using Microsoft's cutting-edge agent protocols and frameworks.

### The Core Scenario

> *"A competitor drops their prices. What do we do?"*

Squad Commerce answers this automatically through a choreographed pipeline of AI agents:

1. **🔍 Detect** — A market intelligence agent spots the competitor price change
2. **📦 Analyze** — The inventory agent queries local stock levels via **MCP** tools
3. **🤝 Verify** — An external vendor agent confirms the competitor's claim via **A2A** protocol
4. **📊 Calculate** — The pricing agent computes margin impact
5. **🖥️ Present** — A native **A2UI** component renders a visual proposal in the Blazor UI
6. **✅ Decide** — The store manager approves or rejects with full auditability

Every step is traced with **OpenTelemetry**, streamed to the UI via **AG-UI**, and auditable through the **Aspire Dashboard**.

---

## 🏗️ Architecture

### Protocol Stack

| Protocol | Purpose | Implementation |
|----------|---------|----------------|
| **MAF** (Microsoft Agent Framework) | Agent orchestration via Graph-based Workflows | .NET 10, `ChiefSoftwareArchitect` as orchestrator |
| **MCP** (Model Context Protocol) | Tool execution for ERP/SQL data access | `GetInventoryLevels`, `UpdateStorePricing` |
| **A2A** (Agent-to-Agent) | Inter-agent and external vendor communication | A2A Handshake protocol with validation |
| **AG-UI** (Agent-to-UI) | Streaming responses to the frontend | `MapAGUI` endpoint, real-time status updates |
| **A2UI** | Generative UI components in Blazor | Rich interactive components (no raw markdown) |

### Agent Architecture

![Squad Commerce Agent Architecture](docs/squad-commerce-architecture.png)

### A2UI Components

Squad Commerce enforces **generative UI** — no raw markdown tables for complex data:

| Component | Renders When | Description |
|-----------|-------------|-------------|
| `RetailStockHeatmap` | Displaying inventory levels | Visual heatmap of stock across locations |
| `PricingImpactChart` | Proposing price changes | Interactive chart showing margin impact |
| `MarketComparisonGrid` | Showing competitor comparisons | Side-by-side competitive analysis |

---

## 🛠️ Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET 10 | 10.0 |
| **Web Framework** | ASP.NET Core | 10.0 |
| **Real-time** | SignalR | 10.0 |
| **Frontend** | Blazor Server + Fluent UI Blazor | v4.14 |
| **Agent Framework** | Microsoft Agent Framework (MAF) | 1.0.0-rc4 |
| **Orchestration** | MAF `WorkflowBuilder` Graph-based Workflows | 1.0.0-rc4 |
| **Tool Protocol** | Model Context Protocol (MCP) — Official C# SDK | 1.1.0 |
| **Agent Communication** | Agent-to-Agent (A2A) | MAF A2A |
| **UI Streaming** | Agent-to-UI (AG-UI) via SSE | Custom |
| **Generative UI** | A2UI JSON payloads → Blazor components | Custom |
| **Observability** | OpenTelemetry + .NET Aspire Dashboard | Aspire 13.2.0 |
| **Identity** | Microsoft Entra ID (scope-based) | — |
| **Database** | SQLite (via EF Core + MCP tools) | EF Core 10.0 |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [.NET Aspire workload](https://learn.microsoft.com/dotnet/aspire)
- Docker Desktop is **optional** — Aspire Dashboard runs natively

### Quick Start

```bash
# Clone the repository
git clone https://github.com/swigerb/squad-commerce.git
cd squad-commerce

# Restore and run with Aspire
dotnet restore
dotnet run --project src/SquadCommerce.AppHost
```

The Aspire Dashboard will open automatically, giving you full visibility into agent orchestration, traces, and metrics.

### ☁️ Deploying to Azure

Squad-Commerce supports **one-command deployment** to Azure Container Apps using Azure Developer CLI (`azd`):

```bash
# Prerequisites: Azure CLI, azd CLI, Docker Desktop
az login
azd up
```

This provisions:
- **Azure Container Registry** (private image registry)
- **Container Apps Environment** with built-in Aspire Dashboard
- **API and Web Container Apps** with HTTPS ingress and service discovery
- **Log Analytics** for centralized observability

**📖 See [docs/DEPLOY.md](docs/DEPLOY.md) for comprehensive deployment guide.**

**💰 Estimated cost:** $5-15/month for demo deployment.

### 📖 Demo Walkthrough

**New to Squad Commerce?** Follow our comprehensive step-by-step demo guide:

👉 **[Read the Full Demo Walkthrough](docs/DEMO.md)** 👈

The demo guide includes:
- ✅ Complete setup instructions
- ✅ Step-by-step scenario walkthrough (competitor price drop)
- ✅ Copy-paste ready cURL commands
- ✅ AG-UI stream examples with real event payloads
- ✅ A2UI component explanations (RetailStockHeatmap, PricingImpactChart, MarketComparisonGrid)
- ✅ Manager approval workflow (Approve/Reject/Modify)
- ✅ Aspire Dashboard navigation (Traces, Metrics, Logs)
- ✅ API reference with all endpoints
- ✅ Demo data reference (5 stores, 8 SKUs)
- ✅ Troubleshooting guide

---

## 🔄 CI/CD Pipeline

Squad Commerce uses **GitHub Actions** for continuous integration and deployment:

### Automated Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **CI** | Push to `main`, PRs | Build, test, Docker image validation |
| **PR Validation** | Pull requests | Quality gates: coverage ≥80%, code formatting |
| **Deploy** | Manual or after CI passes | Deploy to Azure Container Apps via `azd` |

### Build Status

All commits and PRs are automatically built and tested. The build status badge at the top of this README shows the current state of the `main` branch.

### Manual Deployment

Deploy to Azure using the GitHub Actions UI:

1. Go to **Actions** → **Deploy to Azure**
2. Click **Run workflow**
3. Select environment (`production`, `staging`, or `development`)
4. Click **Run workflow**

**Required GitHub Secrets:**
- `AZURE_CLIENT_ID` — Azure service principal client ID
- `AZURE_TENANT_ID` — Azure Active Directory tenant ID
- `AZURE_SUBSCRIPTION_ID` — Azure subscription ID
- `AZURE_LOCATION` — Azure region (defaults to `eastus`)

**Setup OIDC for Azure:**
```bash
# Create Azure AD application for OIDC
az ad app create --display-name "squad-commerce-github-actions"

# Configure federated credentials for GitHub Actions
# See: https://learn.microsoft.com/azure/developer/github/connect-from-azure
```

### Quality Gates

All pull requests must pass:
- ✅ Build succeeds
- ✅ All tests pass (excluding Playwright browser tests)
- ✅ Code coverage ≥80%
- ✅ Code formatting check (`dotnet format`)

---

## 📐 Enterprise Engineering

### Observability

Every agent action, tool call, and handoff is wrapped in an **OpenTelemetry trace**. All internal reasoning steps are logged as structured JSON for full auditability in the Aspire Dashboard.

```
If an executive asks: "How did we decide to lower the price of TVs?"
→ Point to the OpenTelemetry trace and A2UI decision payload
→ See exactly which agent did what, when, and why
```

### Security

- **Entra ID scopes** enforced per agent — no agent can access tools outside its claims
- **AgentPolicy** enforcement in C# ensures compliance at the framework level
- A2A external data is validated against internal telemetry before use

### Agent Policy Enforcement

```csharp
// Real MAF Executor wrapping a domain agent
public sealed class InventoryExecutor(InventoryAgent agent) 
    : Executor<CompetitorPriceDropRequest, AgentResult>("InventoryExecutor")
{
    public override async ValueTask<AgentResult> HandleAsync(
        CompetitorPriceDropRequest request,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        return await agent.ExecuteAsync(request.Sku, ct);
    }
}

// Real MAF WorkflowBuilder graph
var workflow = new WorkflowBuilder(marketIntelExecutor)
    .AddEdge(marketIntelExecutor, inventoryExecutor)
    .AddEdge(inventoryExecutor, pricingExecutor)
    .AddEdge(pricingExecutor, synthesisExecutor)
    .WithOutputFrom(synthesisExecutor)
    .Build();
```

### MCP Tools (Official SDK)

```csharp
[McpServerToolType]
public sealed class GetInventoryLevelsTool(IInventoryRepository repo)
{
    [McpServerTool(Name = "GetInventoryLevels")]
    [Description("Query inventory levels across stores")]
    public async Task<object> ExecuteAsync(
        [Description("Product SKU")] string? sku = null,
        [Description("Store ID")] string? storeId = null,
        CancellationToken ct = default)
    {
        var levels = await repo.GetInventoryLevelsAsync(sku!, ct);
        return new { Success = true, Stores = levels };
    }
}
```

---

## 🤖 The Squad

This project was developed by a team of AI agents managed by [**Squad**](https://github.com/bradygaster/squad) — an AI team orchestration framework created by [**Brady Gaster**](https://github.com/bradygaster). Squad enables multi-agent collaboration where each agent has a persistent identity, memory, and specialized expertise.

Huge shout out to **Brady Gaster** for building Squad — the framework that makes it possible to coordinate a full AI development team with persistent memory, decision tracking, and ceremony-based collaboration. 🙌

### Meet the Team — Microsoft Legends Edition

Our squad is cast from the pantheon of **Microsoft legends** — the people (and paperclip) who built the platforms we all stand on:

| Agent | Role | Inspired By |
|-------|------|-------------|
| 🏗️ **Bill Gates** | Lead | Co-founder of Microsoft. Sees the whole board, thinks in systems. Owns architecture decisions, code review, and scope management. |
| 🔧 **Satya Nadella** | Lead Dev | CEO of Microsoft. Growth mindset applied to every line of code. Owns MAF agent orchestration, A2A protocol, and MCP integration. |
| ⚙️ **Anders** | Backend Dev | Anders Hejlsberg — creator of C# and TypeScript. The type system is your friend. Owns ASP.NET Core, SignalR, and infrastructure plumbing. |
| ⚛️ **Clippy** | User Advocate / AG-UI Expert | The beloved (and redeemed) Office Assistant. "It looks like you're building a UI! Would you like me to make it actually good?" Owns Blazor, A2UI components, and accessibility. |
| 🧪 **Steve Ballmer** | Tester | Former CEO of Microsoft. TESTS! TESTS! TESTS! Owns quality assurance, xUnit test suites, and the 80% coverage floor. |
| 📋 **Scribe** | Session Logger | The silent observer. Documents every decision, logs every session. |
| 🔄 **Ralph** | Work Monitor | Keeps the pipeline moving. Never lets the team sit idle. |

---

## 📁 Project Structure

```
squad-commerce/
├── src/
│   ├── SquadCommerce.AppHost/          # .NET Aspire orchestrator
│   ├── SquadCommerce.ServiceDefaults/  # Shared service configuration
│   ├── SquadCommerce.Web/              # Blazor frontend (A2UI)
│   ├── SquadCommerce.Agents/           # MAF agent definitions
│   ├── SquadCommerce.Mcp/              # MCP server tools (ModelContextProtocol 1.1.0)
│   ├── SquadCommerce.A2A/              # A2A protocol handlers
│   └── SquadCommerce.Contracts/        # Shared models, interfaces, A2UI payloads
├── tests/
│   ├── SquadCommerce.Agents.Tests/     # Agent unit & integration tests
│   ├── SquadCommerce.Mcp.Tests/        # MCP tool tests
│   ├── SquadCommerce.A2A.Tests/        # A2A protocol tests
│   └── SquadCommerce.Web.Tests/        # Blazor bUnit component tests
├── .squad/                             # Squad AI team configuration
├── .github/                            # GitHub workflows
└── README.md
```

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **[Brady Gaster](https://github.com/bradygaster)** — Creator of [Squad](https://github.com/bradygaster/squad), the AI team orchestration framework powering this project's development workflow. Squad makes multi-agent software development real — persistent identities, shared decisions, and ceremony-based collaboration. Thank you, Brady! 🎉
- **Microsoft** — For building the Agent Framework, ASP.NET Core, SignalR, Blazor, Aspire, and the agent protocol stack (A2A, MCP, AG-UI) that makes this possible.
- The **Microsoft Legends** who inspired our squad: Bill Gates, Satya Nadella, Anders Hejlsberg, Steve Ballmer, and yes — Clippy. 📎

---

<p align="center">
  <em>Built with ❤️ by humans and AI agents, orchestrated by Squad</em>
</p>

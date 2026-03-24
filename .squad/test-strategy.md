# Squad-Commerce Test Strategy

> **TESTS! TESTS! TESTS!** If it's not tested, it doesn't work.
> 
> — Steve Ballmer, Tester

## Overview

This document defines the comprehensive testing strategy for **squad-commerce**, a Microsoft showcase demonstrating excellence in AI development using:
- **MAF** (Microsoft Agent Framework)
- **A2A** (Agent-to-Agent communication)
- **MCP** (Model Context Protocol)
- **AG-UI** (Agent-to-UI protocol)
- **A2UI** (Agent-to-User-Interface components in Blazor)
- **ASP.NET Core** + **SignalR** + **Blazor** + **OpenTelemetry** + **Aspire**

Every critical path MUST be tested. Every agent interaction MUST be validated. Every UI component MUST render correctly. This is a Microsoft showcase — we test like it.

---

## 1. Test Layers

### 1.1 Unit Tests

**What Gets Unit Tested:**
- ✅ **Agent Business Logic** — Individual agent decision-making (pricing policies, margin calculations, stock assessment)
- ✅ **MAF Agent Services** — Agent registration, lifecycle management, message routing
- ✅ **A2A Message Builders/Parsers** — Handshake, query, response serialization/deserialization
- ✅ **MCP Tool Definitions** — Tool schema validation, parameter binding, response formatting
- ✅ **A2UI Payload Generators** — Component type emission, data structure validation
- ✅ **Domain Models** — Product, Pricing, Inventory, CompetitorData validation logic
- ✅ **Policies** — Margin calculation policies, pricing recommendation policies, escalation policies
- ✅ **SignalR Hub Methods** — Hub method logic (isolated from connection lifecycle)
- ✅ **Telemetry Emitters** — OpenTelemetry span creation, tag population

**Framework:** xUnit  
**Mocking:** Moq for external dependencies, but prefer real object graphs where possible  
**Isolation:** Fast, in-memory, no network, no database

**Example Test Names:**
```csharp
Should_CalculateCorrectMargin_When_CompetitorPriceDrops30Percent()
Should_GenerateA2UIPayload_When_PricingRecommendationCreated()
Should_EmitTelemetrySpan_When_AgentProcessesQuery()
Should_ThrowValidationException_When_MCPToolParametersMissing()
Should_BuildA2AHandshake_When_InventoryAgentInitiatesContact()
```

---

### 1.2 Integration Tests

**What Gets Integration Tested:**
- ✅ **A2A Communication** — Full roundtrip: Agent A → handshake → Agent B → query → Agent B → response → Agent A
- ✅ **MCP Server/Client** — Real MCP server responds to tool calls, returns structured data
- ✅ **SignalR Hub Communication** — Client connects, subscribes, receives state updates
- ✅ **Database Operations** — EF Core queries, writes, migrations (in-memory or test database)
- ✅ **OpenTelemetry Trace Flow** — Traces propagate across A2A calls, MCP tool invocations, SignalR messages
- ✅ **Entra ID Authorization** — Scope validation, token validation (mock token provider)

**Framework:** xUnit + WebApplicationFactory (ASP.NET Core integration tests)  
**Infrastructure:**
- In-memory databases (EF Core InMemory provider for fast tests)
- TestServer for ASP.NET Core endpoints
- Fake MCP servers (test doubles that implement MCP protocol)
- Fake A2A endpoints (test doubles for external agents)
- SignalR test clients (Microsoft.AspNetCore.SignalR.Client)

**Example Test Names:**
```csharp
Should_CompleteA2AHandshake_When_PricingAgentContactsInventoryAgent()
Should_InvokeMCPTool_When_CatalogAgentQueriesProductDatabase()
Should_BroadcastStateUpdate_When_ManagerApprovesRecommendation()
Should_PropagateTraceContext_When_A2ACallSpansMultipleAgents()
Should_ReturnUnauthorized_When_EntraScopeViolated()
Should_PersistRecommendation_When_DatabaseWriteCompletes()
```

---

### 1.3 End-to-End Tests

**What Gets E2E Tested:**

The **full competitor-price-drop scenario** from detection → analysis → recommendation → approval:

1. **Competitor price drop detected** (external signal or mock)
2. **MCP query to competitor data** → Returns competitor pricing
3. **A2A call to Inventory Agent** → Returns stock levels
4. **A2A call to Catalog Agent** → Returns product details, current pricing
5. **Margin calculation** → Pricing policy evaluates recommendation
6. **A2UI payload generated** → Heatmap, chart, grid components
7. **SignalR push to manager UI** → Blazor components receive and render
8. **Manager approval** → SignalR message back to backend
9. **Price updated** → Database write, confirmation sent
10. **Telemetry trace complete** → OpenTelemetry spans form a coherent trace

**Framework:** xUnit + Playwright (or Selenium) for UI automation  
**Infrastructure:**
- Full application running (ASP.NET Core + SignalR + Blazor)
- Real or staging MCP servers
- Real or staging A2A endpoints
- Real database (test instance)
- OpenTelemetry collector (test instance)

**Example Test Names:**
```csharp
Should_CompleteFullPricingWorkflow_When_CompetitorDropsPrice()
Should_EscalateToHuman_When_MarginThresholdExceeded()
Should_GenerateCompleteTrace_When_E2EWorkflowExecutes()
Should_RenderHeatmap_When_A2UIPayloadReceived()
Should_HandleConcurrentApprovals_When_MultipleManagersAct()
```

---

### 1.4 UI Component Tests

**What Gets Component Tested:**

All **A2UI Blazor components** that render agent responses:
- ✅ **RetailStockHeatmap** — Renders stock levels by region/category
- ✅ **PricingImpactChart** — Visualizes margin impact of pricing changes
- ✅ **MarketComparisonGrid** — Displays competitor pricing vs. current pricing
- ✅ **RecommendationCard** — Shows recommendation summary with action buttons
- ✅ **ApprovalWorkflowComponent** — Manages approval/rejection flow

**Framework:** bUnit (Blazor component testing library)  
**Focus:**
- Component renders with valid A2UI payload
- Correct data binding and display
- User interactions (button clicks, form submissions) trigger correct callbacks
- Component handles missing or malformed data gracefully

**Example Test Names:**
```csharp
Should_RenderHeatmap_When_StockDataProvided()
Should_ShowErrorState_When_A2UIPayloadMalformed()
Should_TriggerApprovalCallback_When_ApproveButtonClicked()
Should_UpdateChart_When_SignalRPushesNewData()
Should_DisplayCompetitorPrices_When_MarketDataAvailable()
```

---

## 2. Test Infrastructure

### 2.1 Test Project Structure

```
squad-commerce/
├── src/
│   ├── SquadCommerce.Agents/           # MAF agents
│   ├── SquadCommerce.A2A/              # A2A protocol implementation
│   ├── SquadCommerce.MCP/              # MCP server/client
│   ├── SquadCommerce.Web/              # ASP.NET Core + SignalR
│   ├── SquadCommerce.Blazor/           # Blazor UI + A2UI components
│   └── SquadCommerce.Shared/           # Domain models, policies
│
├── tests/
│   ├── SquadCommerce.Agents.UnitTests/        # Agent business logic
│   ├── SquadCommerce.A2A.IntegrationTests/    # A2A roundtrip tests
│   ├── SquadCommerce.MCP.IntegrationTests/    # MCP tool invocation tests
│   ├── SquadCommerce.Web.IntegrationTests/    # SignalR + ASP.NET Core
│   ├── SquadCommerce.Blazor.ComponentTests/   # bUnit component tests
│   ├── SquadCommerce.E2E.Tests/               # Full workflow tests
│   └── SquadCommerce.TestHelpers/             # Shared test infrastructure
│
└── .squad/
    └── test-strategy.md                       # This document
```

**Test Helpers Library:**
- `FakeMCPServer` — Test double for MCP protocol
- `FakeA2AAgent` — Test double for external agents
- `TestTelemetryExporter` — Captures OpenTelemetry spans for validation
- `TestSignalRClient` — Simplifies SignalR client setup in tests
- `InMemoryDatabaseFixture` — Shared in-memory database for integration tests
- `A2UIPayloadBuilder` — Fluent builder for test A2UI payloads

---

### 2.2 xUnit as the Framework

**Why xUnit:**
- Industry standard for .NET
- Clean, minimal syntax
- Excellent async/await support
- Parallel test execution by default
- Strong ecosystem (xUnit.DependencyInjection, xUnit.Categories)

**Test Organization:**
- Use `[Fact]` for deterministic tests
- Use `[Theory]` + `[InlineData]` for parameterized tests
- Use `IClassFixture<T>` for shared setup/teardown (databases, servers)
- Use `ICollectionFixture<T>` for shared state across test classes

**Example:**
```csharp
public class PricingAgentTests : IClassFixture<InMemoryDatabaseFixture>
{
    private readonly InMemoryDatabaseFixture _fixture;

    public PricingAgentTests(InMemoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_CalculateCorrectMargin_When_CompetitorPriceDrops30Percent()
    {
        // Arrange
        var agent = new PricingAgent(_fixture.Database, _fixture.Logger);
        var competitorPrice = 70.00m; // 30% drop from 100.00m
        
        // Act
        var recommendation = await agent.EvaluatePricingAsync(competitorPrice);
        
        // Assert
        Assert.NotNull(recommendation);
        Assert.Equal(75.00m, recommendation.RecommendedPrice); // 5-point margin
        Assert.True(recommendation.RequiresApproval);
    }

    [Theory]
    [InlineData(90.00, false)] // 10% drop, auto-approve
    [InlineData(70.00, true)]  // 30% drop, requires approval
    [InlineData(50.00, true)]  // 50% drop, escalate
    public async Task Should_ApplyCorrectApprovalPolicy_When_PriceDropVaries(
        decimal competitorPrice, bool requiresApproval)
    {
        // Arrange & Act & Assert
    }
}
```

---

### 2.3 Test Data Strategy

**Principles:**
- ✅ **Deterministic** — Same input, same output, always
- ✅ **Isolated** — Tests don't depend on each other
- ✅ **Fast** — In-memory where possible, real databases only when necessary
- ✅ **Realistic** — Test data mirrors production shape and constraints

**Approaches:**

1. **In-Memory Databases (Unit/Integration)**
   - Use EF Core InMemory provider
   - Seed test data in fixtures
   - Tear down after test class completes

2. **Mock MCP Servers (Integration)**
   - Implement `IMCPServer` test double
   - Return canned responses for known queries
   - Validate that correct tools are invoked

3. **Fake A2A Endpoints (Integration)**
   - Implement `IA2AAgent` test double
   - Simulate handshake, query, response flow
   - Record interactions for validation

4. **Builders for Complex Objects**
   - Fluent builders for `A2UIPayload`, `A2AMessage`, `MCPToolCall`
   - Default values for optional fields
   - Chainable methods for readability

**Example:**
```csharp
var payload = new A2UIPayloadBuilder()
    .WithComponentType("RetailStockHeatmap")
    .WithData(new { regions = new[] { "West", "East" }, stock = new[] { 100, 50 } })
    .WithTitle("Stock Levels by Region")
    .Build();
```

---

### 2.4 OpenTelemetry Test Trace Validation

**Why Test Telemetry:**
- Traces are critical for production debugging
- Incorrect spans = blind spots in observability
- Context propagation failures = broken traces

**Test Strategy:**
- Use `TestTelemetryExporter` to capture spans in-memory
- Validate span names, attributes, parent-child relationships
- Verify trace context propagates across A2A calls, MCP invocations, SignalR messages

**Example:**
```csharp
[Fact]
public async Task Should_PropagateTraceContext_When_A2ACallSpansMultipleAgents()
{
    // Arrange
    var exporter = new TestTelemetryExporter();
    var tracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddSource("SquadCommerce.*")
        .AddInMemoryExporter(exporter)
        .Build();

    var pricingAgent = new PricingAgent(/* ... */);
    var inventoryAgent = new InventoryAgent(/* ... */);

    // Act
    await pricingAgent.QueryInventoryViaA2A(inventoryAgent);

    // Assert
    var spans = exporter.GetExportedSpans();
    Assert.Equal(3, spans.Count); // Parent + A2A call + Child agent processing
    
    var parentSpan = spans.First(s => s.ParentSpanId == default);
    var childSpan = spans.First(s => s.ParentSpanId == parentSpan.Context.SpanId);
    
    Assert.Equal(parentSpan.Context.TraceId, childSpan.Context.TraceId);
    Assert.Equal("A2A.Query", childSpan.Name);
}
```

---

## 3. Key Test Scenarios

### 3.1 Happy Path Scenarios

| Scenario ID | Name | Description | Test Layer |
|------------|------|-------------|------------|
| **HP-001** | `CompetitorPriceDrop_FullWorkflow` | Competitor drops price 20% → MCP query → A2A inventory check → Recommendation generated → A2UI payload created → Manager approves → Price updated | E2E |
| **HP-002** | `A2AHandshake_Success` | Agent A initiates handshake → Agent B accepts → Session established → Query sent → Response received | Integration |
| **HP-003** | `MCPToolInvocation_ReturnsData` | Catalog agent invokes MCP tool `GetCompetitorPricing` → Tool returns JSON → Agent parses successfully | Integration |
| **HP-004** | `A2UIPayload_RendersHeatmap` | Blazor component receives A2UI payload → Heatmap renders with correct data → User sees stock levels by region | Component |
| **HP-005** | `SignalRStateUpdate_PropagatesChange` | Backend updates recommendation → SignalR broadcasts → All connected clients receive update → UI reflects change | Integration |

---

### 3.2 Error Handling Scenarios

| Scenario ID | Name | Description | Test Layer |
|------------|------|-------------|------------|
| **EH-001** | `MCPToolError_GracefulEscalation` | MCP tool returns error (timeout, invalid data) → Agent logs error → Escalates to human operator → No crash | Integration |
| **EH-002** | `A2AHandshakeFails_FallbackBehavior` | Agent A attempts handshake → Agent B unreachable → Agent A retries with backoff → Eventually falls back to local cache | Integration |
| **EH-003** | `A2UIPayloadMalformed_ComponentShowsError` | Blazor component receives malformed A2UI payload → Validation fails → Error boundary catches → User sees friendly error message | Component |
| **EH-004** | `SignalRDisconnect_AutoReconnect` | Client loses connection → SignalR retries → Connection restored → Client receives missed updates | Integration |
| **EH-005** | `MarginThresholdExceeded_RequiresApproval` | Pricing recommendation exceeds margin threshold → Requires manager approval → Cannot auto-apply → Notification sent | Unit |

---

### 3.3 Authorization & Security Scenarios

| Scenario ID | Name | Description | Test Layer |
|------------|------|-------------|------------|
| **SEC-001** | `EntraScopeViolation_AccessDenied` | User without `pricing.write` scope attempts to approve → Authorization middleware blocks → Returns 403 Forbidden | Integration |
| **SEC-002** | `A2ATokenValidation_RejectsInvalid` | Agent A sends A2A message with invalid token → Agent B validates token → Rejects message → Returns 401 Unauthorized | Integration |
| **SEC-003** | `MCPToolPermission_RestrictedAccess` | Agent without `catalog.read` permission invokes MCP tool → MCP server checks permissions → Returns 403 | Integration |
| **SEC-004** | `SignalRGroupIsolation_NoLeakage` | Manager A in group "Region-West" → Manager B in group "Region-East" → Update for West → Only A receives, B does not | Integration |

---

### 3.4 Performance & Concurrency Scenarios

| Scenario ID | Name | Description | Test Layer |
|------------|------|-------------|------------|
| **PERF-001** | `ConcurrentApprovals_NoRaceCondition` | Two managers approve same recommendation simultaneously → Optimistic concurrency control → Only one succeeds, other gets conflict error | Integration |
| **PERF-002** | `HighVolumeSignalRBroadcast_NoDelay` | 100 price updates/second → SignalR broadcasts to 50 clients → All clients receive within 500ms | E2E |
| **PERF-003** | `A2AQueryBatching_ReducesLatency` | Agent batches 10 A2A queries → Sends as single batch → Receives batch response → Total latency < individual queries | Integration |

---

### 3.5 Telemetry & Observability Scenarios

| Scenario ID | Name | Description | Test Layer |
|------------|------|-------------|------------|
| **OBS-001** | `TraceContextPropagation_AcrossA2A` | Agent A starts trace → Calls Agent B via A2A → Agent B continues trace → Spans linked by TraceId | Integration |
| **OBS-002** | `MCPToolInvocation_EmitsSpan` | Agent invokes MCP tool → Span created with `mcp.tool.name` attribute → Span duration recorded → Span exported | Unit |
| **OBS-003** | `SignalRMessage_EmitsSpan` | SignalR hub sends message → Span created → Contains `signalr.method` attribute → Linked to parent trace | Integration |
| **OBS-004** | `E2EWorkflow_CompleteTrace` | Full competitor price drop workflow → Single trace with 15+ spans → All spans linked → Trace visualizable in Jaeger | E2E |

---

### 3.6 Data Validation Scenarios

| Scenario ID | Name | Description | Test Layer |
|------------|------|-------------|------------|
| **VAL-001** | `A2UIPayload_SchemaValidation` | A2UI payload generated → Validated against JSON schema → Missing required field → ValidationException thrown | Unit |
| **VAL-002** | `MCPToolParameters_TypeChecking` | MCP tool expects `int` parameter → Agent sends `string` → Type conversion fails → Returns 400 Bad Request | Integration |
| **VAL-003** | `A2AMessage_RequiredFieldsPresent` | A2A message created → `MessageId`, `From`, `To` required → Missing field → Serialization fails with clear error | Unit |

---

## 4. Coverage Targets

### 4.1 Per-Project Coverage Goals

| Project | Minimum Coverage | Critical Path Coverage | Notes |
|---------|-----------------|----------------------|-------|
| **SquadCommerce.Agents** | 80% | 100% | Agent business logic is critical — zero tolerance for gaps |
| **SquadCommerce.A2A** | 85% | 100% | A2A protocol must be bulletproof |
| **SquadCommerce.MCP** | 85% | 100% | MCP tool invocation is core functionality |
| **SquadCommerce.Web** | 75% | 95% | SignalR hubs + API endpoints |
| **SquadCommerce.Blazor** | 70% | 90% | A2UI components — visual testing also required |
| **SquadCommerce.Shared** | 90% | 100% | Domain models, policies — heavily reused |

**Overall Target:** **80% code coverage minimum, 100% on critical paths.**

---

### 4.2 Critical Paths That MUST Have 100% Coverage

1. **Pricing Recommendation Calculation** — Incorrect pricing = lost revenue
2. **A2A Handshake & Message Routing** — Failures break agent collaboration
3. **MCP Tool Invocation & Response Parsing** — Incorrect data = bad decisions
4. **Entra ID Authorization** — Security vulnerability if broken
5. **A2UI Payload Generation** — Malformed payloads = broken UI
6. **SignalR State Broadcast** — Missed updates = stale UI
7. **OpenTelemetry Trace Emission** — Broken traces = blind production debugging

---

## 5. Quality Gates

### 5.1 What Blocks a PR from Merging

❌ **PR BLOCKED IF:**
- Any test fails
- Code coverage drops below project threshold
- New public API added without corresponding test
- Critical path not covered by tests
- Test names don't follow naming convention
- Integration tests skipped without justification
- E2E tests disabled without architectural review

✅ **PR APPROVED IF:**
- All tests pass
- Coverage meets or exceeds threshold
- New features include unit + integration tests
- Critical paths have 100% coverage
- Test names are descriptive and follow convention
- Code review checklist completed

---

### 5.2 Test Naming Conventions

**Format:** `Should_<ExpectedBehavior>_When_<Condition>`

**Examples:**
```csharp
// ✅ GOOD
Should_CalculateCorrectMargin_When_CompetitorPriceDrops30Percent()
Should_ReturnUnauthorized_When_EntraScopeViolated()
Should_PropagateTraceContext_When_A2ACallSpansMultipleAgents()

// ❌ BAD
Test1()
TestPricing()
VerifyMarginCalculation()  // Not clear what the expectation is
```

**Why:**
- Test names are documentation
- Failures are immediately understandable
- Grep-able for specific behaviors

---

### 5.3 Test Review Checklist

**For the Author:**
- [ ] All new public APIs have tests
- [ ] Happy path + error handling both tested
- [ ] Test names follow `Should_X_When_Y` convention
- [ ] No `Thread.Sleep()` in tests (use proper async waits)
- [ ] No hardcoded timeouts < 5 seconds (CI can be slow)
- [ ] Test data is deterministic and isolated
- [ ] Telemetry spans validated where applicable
- [ ] Integration tests use TestServer or in-memory infrastructure

**For the Reviewer:**
- [ ] Tests actually validate the behavior (not just "green")
- [ ] Edge cases covered (null inputs, empty collections, boundary conditions)
- [ ] Tests are maintainable (not brittle, not over-mocked)
- [ ] Test readability: Arrange/Act/Assert clear
- [ ] No test interdependencies (each test can run in isolation)

---

## 6. Test Execution

### 6.1 Local Development

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SquadCommerce.Agents.UnitTests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run E2E tests (slower, run before commit)
dotnet test tests/SquadCommerce.E2E.Tests/ --filter "Category=E2E"
```

---

### 6.2 CI/CD Pipeline

**On PR:**
1. Run all unit tests (fast feedback)
2. Run all integration tests (A2A, MCP, SignalR)
3. Generate coverage report
4. Fail if coverage below threshold
5. Run E2E tests on staging environment

**On Merge to Main:**
1. Run full test suite
2. Generate coverage report
3. Publish to telemetry dashboard
4. Deploy to staging if all tests pass

---

## 7. Test Maintenance

### 7.1 Test Hygiene

- **Delete obsolete tests** — If the feature is removed, remove the test
- **Update tests with code** — Tests are first-class code, maintain them
- **Refactor duplicate setup** — Use fixtures, builders, helpers
- **Keep tests fast** — Slow tests = ignored tests

---

### 7.2 Test Debt

**Allowed:**
- Performance tests for non-critical paths can be marked as `[Fact(Skip = "Performance test")]` temporarily

**Not Allowed:**
- Skipping critical path tests
- Disabling integration tests "because they're flaky" (fix them instead)
- Commenting out failing tests (fix or delete)

---

## 8. Test Ownership

| Test Type | Owner | Reviewer |
|-----------|-------|----------|
| Unit Tests (Agents) | Anders (Backend Dev) | Steve Ballmer (Tester) |
| A2A Integration Tests | Satya Nadella (Lead Dev) | Steve Ballmer (Tester) |
| MCP Integration Tests | Satya Nadella (Lead Dev) | Steve Ballmer (Tester) |
| Blazor Component Tests | Clippy (User Advocate) | Steve Ballmer (Tester) |
| E2E Tests | Steve Ballmer (Tester) | Bill Gates (Lead) |
| Test Infrastructure | Steve Ballmer (Tester) | Satya Nadella (Lead Dev) |

---

## 9. Success Metrics

**Weekly:**
- Zero failing tests in main branch
- 80%+ code coverage across all projects
- 100% coverage on critical paths
- < 5% test skip rate

**Monthly:**
- < 5 production bugs that could have been caught by tests
- Test suite execution time < 10 minutes (local)
- E2E test suite execution time < 30 minutes (CI)

---

## 10. Appendix: Test Tools & Libraries

| Tool/Library | Purpose |
|--------------|---------|
| **xUnit** | Test framework |
| **Moq** | Mocking library |
| **FluentAssertions** | Readable assertions |
| **bUnit** | Blazor component testing |
| **WebApplicationFactory** | ASP.NET Core integration tests |
| **Microsoft.AspNetCore.SignalR.Client** | SignalR test clients |
| **Testcontainers** | Docker containers for integration tests (optional) |
| **Playwright** | E2E UI automation |
| **Coverlet** | Code coverage |
| **ReportGenerator** | Coverage report visualization |

---

## Closing Statement

**This is a Microsoft showcase.**  
**We test like our reputation depends on it — because it does.**  
**Every agent interaction. Every MCP tool call. Every A2UI component. Every SignalR broadcast.**  
**TESTED. VALIDATED. VERIFIED.**

**If it's not tested, it doesn't ship.**

— Steve Ballmer, Tester  
— squad-commerce team

---

**Last Updated:** 2026-03-24  
**Reviewed By:** Bill Gates (Lead), Satya Nadella (Lead Dev)  
**Status:** ✅ Approved

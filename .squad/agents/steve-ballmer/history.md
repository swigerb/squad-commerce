# Project Context

- **Owner:** Brian Swiger
- **Project:** squad-commerce — A sample commerce application demonstrating Microsoft Agent Framework (MAF), A2A, MCP, AG-UI, and A2UI
- **Stack:** ASP.NET Core, SignalR, Blazor (A2UI), C#, Microsoft Agent Framework, MCP, A2A, AG-UI
- **Created:** 2026-03-24

## Core Context

Tester for squad-commerce. Responsible for unit tests, integration tests, and quality assurance across all agent communication protocols (A2A, MCP) and commerce workflows. Uses xUnit and focuses on integration tests over mocks for agent interactions.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-24: Test Strategy Established

Created comprehensive test strategy for squad-commerce at `.squad/test-strategy.md`. This is a Microsoft showcase — EVERY critical path gets 100% coverage.

**Test Architecture Decisions:**
- xUnit as the exclusive test framework (parallel execution, async/await support)
- Integration tests over mocks for A2A and MCP communication — real protocol implementations catch real bugs
- 80% minimum coverage across all projects, 100% on critical paths (pricing, A2A, MCP, authorization, A2UI, telemetry)
- OpenTelemetry trace validation in all integration/E2E tests — broken traces = blind production debugging
- Test naming: `Should_<ExpectedBehavior>_When_<Condition>` — test names are documentation
- Per-component test projects: `<SourceProject>.<TestType>Tests` structure
- In-memory databases (EF Core InMemory) for fast integration tests
- bUnit for Blazor A2UI component testing
- Playwright for E2E UI automation (headless mode in CI)
- Test review checklist in PR template — quality gates enforced

**Key Test Scenarios (15 scenarios defined):**
- Happy path: competitor price drop → full workflow → manager approval
- Error handling: MCP tool errors, A2A handshake failures, malformed A2UI payloads
- Security: Entra ID scope violations, A2A token validation, MCP permission checks
- Performance: concurrent approvals, high-volume SignalR broadcasts
- Observability: trace context propagation across A2A, MCP, SignalR
- Data validation: A2UI payload schema, MCP tool parameter types, A2A message fields

**Test Infrastructure:**
- `SquadCommerce.TestHelpers` — shared test infrastructure library
- `FakeMCPServer` — test double for MCP protocol (not a mock)
- `FakeA2AAgent` — test double for A2A protocol (not a mock)
- `TestTelemetryExporter` — captures OpenTelemetry spans for validation
- `TestSignalRClient` — simplifies SignalR client setup
- `InMemoryDatabaseFixture` — shared in-memory database for integration tests
- `A2UIPayloadBuilder` — fluent builder for test payloads

**Critical Paths with 100% Coverage Requirement:**
1. Pricing recommendation calculation
2. A2A handshake & message routing
3. MCP tool invocation & response parsing
4. Entra ID authorization
5. A2UI payload generation
6. SignalR state broadcast
7. OpenTelemetry trace emission

**Quality Gates:**
- PR blocked if: tests fail, coverage drops below threshold, critical paths not covered, test naming doesn't follow convention
- Test review checklist: validates test quality, naming, isolation, telemetry validation

**Test Ownership:**
- Unit tests (Agents): Anders (Backend Dev) | Reviewer: Steve Ballmer
- A2A/MCP integration tests: Satya Nadella (Lead Dev) | Reviewer: Steve Ballmer
- Blazor component tests: Clippy (User Advocate) | Reviewer: Steve Ballmer
- E2E tests: Steve Ballmer | Reviewer: Bill Gates
- Test infrastructure: Steve Ballmer | Reviewer: Satya Nadella

**File Paths:**
- `.squad/test-strategy.md` — comprehensive test strategy document
- `.squad/decisions/inbox/steve-ballmer-test-strategy.md` — test architecture decisions for team review

**Next Steps:**
1. Team review and approval of test strategy
2. Create initial test projects (SquadCommerce.Agents.UnitTests, SquadCommerce.TestHelpers)
3. Implement test doubles (FakeMCPServer, FakeA2AAgent)
4. Configure CI/CD with coverage gates (80% minimum, fail on drop)
5. Add test review checklist to PR template

This is the foundation for WORLD-CLASS quality. If it's not tested, it doesn't ship.

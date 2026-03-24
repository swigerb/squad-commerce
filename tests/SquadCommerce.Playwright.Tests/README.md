# SquadCommerce Playwright E2E Tests

This project contains end-to-end browser automation tests for the SquadCommerce Blazor UI using Playwright.

## Overview

These tests validate the full competitor price analysis workflow, including:
- **Main layout and navigation**
- **Agent chat interaction**
- **A2UI component rendering** (heatmaps, charts, grids, audit trail, pipeline)
- **Manager approval workflow** (approve/reject/modify pricing)
- **Accessibility compliance** (WCAG 2.1)
- **Responsive design** (mobile, tablet, desktop)

## Prerequisites

1. **.NET 10.0 SDK** installed
2. **Playwright browsers** installed (see Installation section)
3. **Running application servers** (API and Web) - see Running Tests section

## Installation

### 1. Restore NuGet packages

```powershell
dotnet restore
```

### 2. Install Playwright browsers

```powershell
# After building the project
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

Or use the Playwright CLI:

```powershell
npx playwright install
```

This installs Chromium, Firefox, and WebKit browsers required by Playwright.

## Running Tests

### Option 1: With External Servers Running

Start the API and Web servers manually, then run tests:

```powershell
# Terminal 1: Start API
dotnet run --project src/SquadCommerce.Api/SquadCommerce.Api.csproj

# Terminal 2: Start Web
dotnet run --project src/SquadCommerce.Web/SquadCommerce.Web.csproj

# Terminal 3: Run tests
dotnet test tests/SquadCommerce.Playwright.Tests/
```

### Option 2: With Auto-Start (Experimental)

The `TestServerFixture` can attempt to start servers automatically:

```powershell
dotnet test tests/SquadCommerce.Playwright.Tests/
```

**Note:** Some tests may fail or warn if servers are not running. This is expected behavior.

## Test Categories

Run specific test categories using filters:

```powershell
# Smoke tests (quick validation)
dotnet test --filter Category=Smoke

# E2E workflow tests
dotnet test --filter Category=E2E

# Accessibility tests
dotnet test --filter Category=Accessibility

# Responsive design tests
dotnet test --filter Category=Responsive

# UI-only tests (no backend required)
dotnet test --filter Category=UI
```

## Configuration

### Environment Variables

- `TEST_WEB_URL` - Web app base URL (default: `https://localhost:7000`)
- `TEST_API_URL` - API base URL (default: `https://localhost:7001`)
- `HEADED` - Run browser in headed mode for debugging (default: `false`)
- `CI` - Enable CI mode (records videos on failure)

Example:

```powershell
$env:TEST_WEB_URL = "https://localhost:5001"
$env:HEADED = "true"
dotnet test
```

## Project Structure

```
SquadCommerce.Playwright.Tests/
├── Pages/                          # Page Object Models
│   ├── MainPage.cs                 # Main layout page
│   ├── AgentChatPage.cs            # Chat panel page
│   ├── A2UIComponentsPage.cs       # A2UI visualization components
│   └── ApprovalPanelPage.cs        # Approval workflow page
├── Tests/                          # Test scenarios
│   ├── HomePageTests.cs            # Home page and layout tests
│   ├── CompetitorAnalysisE2ETests.cs # Full workflow tests
│   ├── ManagerDecisionE2ETests.cs  # Approval workflow tests
│   ├── AccessibilityTests.cs       # WCAG compliance tests
│   └── ResponsiveTests.cs          # Responsive design tests
├── Helpers/                        # Test utilities
│   └── TestServerFixture.cs        # Server startup/teardown
├── Fixtures/                       # Base test classes
│   └── PlaywrightTestBase.cs      # Playwright setup/teardown
└── test-results/                   # Test output (generated)
    ├── screenshots/                # Screenshots on failure
    ├── traces/                     # Playwright traces
    └── videos/                     # Video recordings (CI only)
```

## Page Object Model Pattern

Tests use the Page Object Model (POM) pattern for maintainability:

```csharp
// Example: Using MainPage POM
var mainPage = new MainPage(page, baseUrl);
await mainPage.NavigateAsync();
await mainPage.WaitForAppLoadedAsync();

var isLayoutVisible = await mainPage.IsLayoutVisibleAsync();
Assert.That(isLayoutVisible, Is.True);
```

## Debugging Failed Tests

### 1. Screenshots

Screenshots are automatically captured on test failure and saved to `test-results/screenshots/`.

### 2. Playwright Traces

Trace files (`.zip`) are saved to `test-results/traces/`. Open them with:

```powershell
npx playwright show-trace test-results/traces/TestName_20260324_120000.zip
```

This opens the Playwright Trace Viewer with a timeline, network activity, and DOM snapshots.

### 3. Headed Mode

Run tests in headed (visible) browser mode:

```powershell
$env:HEADED = "true"
dotnet test
```

### 4. Run Single Test

```powershell
dotnet test --filter "FullyQualifiedName~HomePageTests.Should_LoadMainLayout_When_AppStarts"
```

## Test Strategy Alignment

These tests implement decision **T9** from `.squad/test-strategy.md`:

- **Framework:** Playwright for cross-browser E2E automation
- **Scope:** Full application stack (Blazor UI + API + SignalR)
- **Coverage:** Competitor price drop workflow from detection to approval
- **Validation:** A2UI component rendering, manager decisions, telemetry

See `.squad/test-strategy.md` for complete testing strategy.

## Known Limitations

1. **Server Dependency:** Most E2E tests require running backend services. Tests gracefully warn if servers are unavailable.
2. **Browser Install:** Playwright browsers are ~500MB. CI environments need to cache or install during setup.
3. **Timing Sensitivity:** Some tests use generous timeouts (30s) for async workflows. Adjust as needed.
4. **CSS Selectors:** Tests use CSS classes from Blazor components. If component structure changes, locators may need updates.

## CI/CD Integration

Example GitHub Actions workflow:

```yaml
- name: Install Playwright browsers
  run: pwsh tests/SquadCommerce.Playwright.Tests/bin/Debug/net10.0/playwright.ps1 install

- name: Start application servers
  run: |
    dotnet run --project src/SquadCommerce.Api/SquadCommerce.Api.csproj &
    dotnet run --project src/SquadCommerce.Web/SquadCommerce.Web.csproj &
    sleep 10

- name: Run Playwright tests
  env:
    CI: true
  run: dotnet test tests/SquadCommerce.Playwright.Tests/ --logger "trx"

- name: Upload test results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: playwright-results
    path: tests/SquadCommerce.Playwright.Tests/test-results/
```

## Contributing

When adding new tests:
1. Use the POM pattern - update or create Page Object classes
2. Follow naming convention: `Should_ExpectedBehavior_When_Condition`
3. Add appropriate test categories (`[Category("E2E")]`, etc.)
4. Include assertions with descriptive messages
5. Handle timeouts gracefully with `Assert.Warn()` for backend-dependent tests

## Resources

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [NUnit Documentation](https://nunit.org/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)

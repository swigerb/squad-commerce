# Playwright Configuration

# This file documents the Playwright test configuration for SquadCommerce E2E tests.
# Playwright for .NET uses attributes and code-based configuration rather than playwright.config.ts

## Configuration Options (Applied in PlaywrightTestBase.cs)

### Browser Settings
- Browser: Chromium (headless by default)
- Headless mode: Controlled by HEADED environment variable
- Viewport: 1280x720 (desktop), adjustable per test
- SlowMo: 0ms (no artificial delays)

### Timeouts
- Default timeout: 30 seconds (Page.SetDefaultTimeout)
- Navigation timeout: 30 seconds
- Action timeout: 30 seconds (click, fill, etc.)

### Test Artifacts
- Screenshots: Captured on test failure → test-results/screenshots/
- Traces: Recorded with screenshots, snapshots, sources → test-results/traces/
- Videos: Recorded only in CI mode (CI=true) → test-results/videos/

### Context Options
- Ignore HTTPS errors: true (for local dev with self-signed certs)
- User-Agent: Default Chromium user agent
- Locale: Default system locale
- Timezone: Default system timezone

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| TEST_WEB_URL | Base URL for Web app | https://localhost:7000 |
| TEST_API_URL | Base URL for API | https://localhost:7001 |
| HEADED | Run browser in headed mode | false |
| CI | Enable CI mode (video recording) | false |

## Browser Installation

Browsers are installed via Playwright CLI:

```powershell
# After building the test project
pwsh bin/Debug/net10.0/playwright.ps1 install
```

This installs:
- Chromium (default for tests)
- Firefox (available if needed)
- WebKit (available if needed)

## Test Execution Modes

### Headless (Default)
```powershell
dotnet test
```

### Headed (Visible Browser)
```powershell
$env:HEADED = "true"
dotnet test
```

### CI Mode (Video Recording)
```powershell
$env:CI = "true"
dotnet test
```

## Debugging

### View Trace Files
```powershell
npx playwright show-trace test-results/traces/TestName_20260324_120000.zip
```

### Run Single Test in Headed Mode
```powershell
$env:HEADED = "true"
dotnet test --filter "FullyQualifiedName~Should_LoadMainLayout_When_AppStarts"
```

## Cross-Browser Testing

To test with Firefox or WebKit, modify PlaywrightTestBase.cs:

```csharp
// Change from:
Browser = await Playwright.Chromium.LaunchAsync(new() { ... });

// To Firefox:
Browser = await Playwright.Firefox.LaunchAsync(new() { ... });

// To WebKit:
Browser = await Playwright.Webkit.LaunchAsync(new() { ... });
```

## Performance Tuning

- **Parallel Execution:** NUnit supports parallel test execution. Use `[Parallelizable]` attribute.
- **Browser Context Reuse:** Each test gets a new context for isolation (recommended).
- **Timeout Adjustment:** Increase for slow environments, decrease for fast CI.

## References
- [Playwright .NET API](https://playwright.dev/dotnet/docs/api/class-playwright)
- [Playwright Test Fixtures](https://playwright.dev/dotnet/docs/test-runners)

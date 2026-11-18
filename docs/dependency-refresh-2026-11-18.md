# Dependency refresh — 2026-11-18

## Status and source evidence

The dependency refresh now includes the **Aspire 17 AppHost source migration**.
Local development/compile checks passed; signed production-package validation
remains held as detailed below.
No application version, Squad/template state, tracked build output, deployment
artifact, remote branch, PR, issue, or cloud resource was changed.

Every direct NuGet package was independently checked against the configured
`azure-default` service index:
`https://packagefeedproxy.microsoft.io/nuget/v3/index.json`.
Its advertised `PackageBaseAddress/3.0.0` endpoint supplied version lists and
package manifests. Public NuGet remained disabled; no feed configuration changed.
Stable versions were selected by version, not by the order returned by the feed.

The official [Aspire 17 release notes](https://aspire.dev/whats-new/aspire-17/)
and [project migration guide](https://aspire.dev/integrations/dotnet/migrate-dotnet-project-resource/)
were reviewed. During the earlier refresh the feed listed 13.6.5 as the newest stable AppHost SDK, AppHost
package and CLI, and no stable `Aspire.Hosting.Dotnet`. Exact 17.0.0 `.nuspec`
requests for `Aspire.Hosting.AppHost`, `Aspire.Hosting.Dotnet`, and `Aspire.Cli`
each returned **HTTP 404**. These were availability failures, not policy denials;
no public-source fallback was attempted. This availability scan was not repeated
for the bounded migration. The separate release-commit validation below does not
establish that signed 17.0.0 packages are available from the approved feed.

## Direct versions

| Dependency | Selected version |
| --- | --- |
| .NET SDK / target frameworks | 10.0.404 / net10.0 |
| AppHost SDK | Microsoft.NET.Sdk (obsolete Aspire.AppHost.Sdk removed) |
| Aspire.Hosting.AppHost; Aspire.Hosting.Dotnet | 17.0.0 (local Debug CI artifacts for compile validation) |
| Aspire CLI / dashboard / DCP bundle | 17.0.0 released CLI |
| Microsoft.Agents.AI; Microsoft.Agents.AI.Workflows | 1.28.0 |
| Microsoft.EntityFrameworkCore.Design; .Sqlite; .InMemory | 10.0.15 |
| Microsoft.Extensions.DependencyInjection.Abstractions; Logging.Abstractions | 11.0.0 |
| Microsoft.Extensions.Http.Resilience; ServiceDiscovery | 11.0.0 |
| ModelContextProtocol; ModelContextProtocol.AspNetCore | 2.4.0 |
| OpenTelemetry.Exporter.OpenTelemetryProtocol; Extensions.Hosting | 1.21.0 |
| OpenTelemetry.Instrumentation.AspNetCore; .Http; .Runtime | 1.21.0 |
| Microsoft.AspNetCore.SignalR.Client | 11.0.0 |
| Microsoft.FluentUI.AspNetCore.Components; .Icons | 4.14.5 |
| Vendored Bootstrap CSS/JS | 5.3.3 (npm traffic hold below) |
| bunit | 2.15.2 |
| coverlet.collector | 10.1.0 |
| FluentAssertions | 8.11.0 |
| Microsoft.NET.Test.Sdk | 18.12.0 |
| Moq | 4.21.0 |
| xunit | 2.9.3 (already latest for this package ID) |
| xunit.runner.visualstudio | 4.0.1 |
| Microsoft.Playwright; Microsoft.Playwright.NUnit | 1.65.0 |
| NUnit / NUnit.Analyzers / NUnit3TestAdapter | 5.0.0 / 4.15.0 / 6.3.1 |
| Repository-local dotnet-ef | 10.0.15 |
| API/Web SDK container / ASP.NET container | 10.0.404 / 10.0.15 |

NuGet manifests confirm the selected Extensions 11 packages expose net10.0
assets; the SignalR 11 client exposes compatible .NET Standard assets. Their
major versions do not change this solution's target framework to .NET 11.
No transitive package was independently overridden.

GitHub Actions' stable release endpoints and tag-to-commit references were
checked read-only. Workflows pin the verified commits for checkout 7.0.1,
github-script 9.0.0, setup-dotnet 6.1.0, upload-artifact 7.0.2, azure/login 3.1.0,
dorny/test-reporter 3.0.0, and sticky-pull-request-comment 3.0.5.
CodeCoverageSummary 1.3.0 is already its latest stable release.
No workflow was dispatched. Official MCR tag lists verified both .NET 10 image
tags; no images were pulled or built.

## Evidence-based holds

- **Signed Aspire 17 package validation:** still held by the earlier exact
  approved-feed availability failures. The source now uses resource v2; a
  matching released CLI and isolated release-commit Debug packages established
  local compile compatibility, not signed production-package availability.
  No CLI, SDK, workload, PATH or NuGet configuration was installed/changed globally.
- **EF Core and ASP.NET Core Mvc.Testing 10.0.15; dotnet-ef 10.0.15:** their 11.0.0
  releases require net11.0. Retaining .NET 10 is intentional, not an update to
  preview or an unrequested framework migration.
- **Fluent UI 4.14.5:** 5.1.0 was restored and compilation tested. It breaks the
  current UI contract: SettingsPanel reports RZ9991 for `@bind-Value` and RZ10000
  for `FluentSelect`'s new TValue parameter; MainLayout/ApprovalPanel no longer
  resolve FontWeight; button/badge Appearance values require new specialized
  enums. Both Components and Icons were returned to independently verified
  latest stable 4.x, preserving the existing Blazor rendering contract. No UI
  redesign or silent binding changes were made. Upstream now treats v4 as legacy
  security-maintenance-only; a tested v5 component migration remains necessary.
- **Vendored Bootstrap 5.3.3:** App.razor actively loads its local CSS. An offline
  npm configuration inspection confirmed the approved effective proxy and no
  project lockfile. A metadata-only, zero-retry Bootstrap lookup verified 5.3.9
  as latest stable, but its advertised tarball is on
  `ms-feed-25.pkgs.visualstudio.com`, outside the user's sole permitted npm URL
  prefix. No package download or off-allowlist request was made; vendored assets
  were not silently replaced through another distribution channel.
- **Squad dependencies/templates:** intentionally untouched; only third-party
  Action version pins in their workflows changed. Example MCP configuration is
  scaffold, not an installed application dependency, and was not activated.
- **NuGet lockfiles:** no pre-existing tracked NuGet locks were found. The
  newly generated AppHost development lock was removed from candidate changes,
  along with the validation-only opt-in for creating it. Unsigned Debug CI
  `.nupkg` content hashes **differ** from signed Release packages with the same
  17.0.0 IDs/versions. They cannot be portable signed-release locks or inputs to
  CI `--locked-mode`. Generate/review any release lock only after approved signed
  packages arrive; do not relabel hashes, ignore mismatches or publish local
  artifacts. Existing locking policy and other source changes are preserved.
  Future local checks must keep development-only locks outside candidate changes.
  No tracked bin/obj output was refreshed.

The npm traffic constraint applies to vendored package acquisition too:

> Use ONLY https://packagefeedproxy.microsoft.io/npm/ for npm-family package traffic. NEVER contact registry.npmjs.org, including probes, direct downloads, redirects, or lockfile-resolved tarball URLs. No public-registry overrides or fallbacks, including --registry, environment variables, scoped registry settings, or alternate configuration files. Inspect effective registry and lockfiles OFFLINE before network/install; do not run install/ci that could reach unapproved registry. If IT policy blocks a request, STOP immediately, NEVER retry even once; report sanitized package/command/error. Do not silently rewrite lockfiles to evade restriction. Other feeds respect configured approved sources; npm proxy not for NuGet/Python/etc.

The Fluent UI script reference now uses the package-supplied stable
`Microsoft.FluentUI.AspNetCore.Components.lib.module.js` entry point documented
in the [4.14.5 upstream README](https://github.com/microsoft/fluentui-blazor/blob/v4.14.5/README.md#script).
The restored package asset inventory confirms that module exists; the old
hard-coded `js/web-components-v2.5.16.min.js` path does not. This keeps script
loading aligned with the selected NuGet package rather than independently
pinning an incompatible web-components payload.

## Aspire 17 source migration

The AppHost now uses `Microsoft.NET.Sdk` with explicit `Aspire.Hosting.AppHost`
and `Aspire.Hosting.Dotnet` 17.0.0 references. The old project references were
checked against their actual paths before migrating:

| Resource | Path relative to src\SquadCommerce.AppHost |
| --- | --- |
| api | ..\SquadCommerce.Api\SquadCommerce.Api.csproj |
| web | ..\SquadCommerce.Web\SquadCommerce.Web.csproj |

Both registrations now use path-based `AddDotnetProject` with portable
`Path.Combine` paths. Their orchestration-only ProjectReferences were removed.
No other
generated Projects metadata consumers or specialized Blazor gateway registrations
were found. Default launch-profile selection, `api`/`web` names, external HTTP
endpoints, web's API reference and `WaitFor`, UserSecretsId, and Blazor Server's
existing application references/publish settings are unchanged. This is a
regular Blazor Server project, not a specialized gateway or WebAssembly
metadata reference. Both services remain in the solution. All consumers remain
`net10.0`; there was no unrelated framework migration.

Aspire 17 configures .NET SDK container publishing for `AddDotnetProject`
automatically, but this has **not** been publish-validated here. Use
`aspire publish` when authorized, not legacy `dotnet run --operation publish`.
CI must provision the matching released CLI 17 bundle before AppHost builds and
use approved signed packages with reviewed locks. No local Debug CI package
source, machine-absolute source, or expiring artifact download URL is committed.

### Isolated local validation inputs

The official GitHub Windows x64 CLI reports
`17.0.0+2d325b13709752ba6d9a943635c5e2940a2530c0`. Its Authenticode signature and
SHA512 sidecar were verified; archive SHA256:
`3d88b09be9cd3d344323b2e3531280954953581adb8102ad50eaf23c67007c05`.
Matching-commit NuGets came from GitHub Actions run **44203911604**, artifact
**14382652960**, `built-nugets`, archive SHA256:
`6e522a103ff37924122e9ebea3a8bf33f21b3349e11e0b59bc86ac51cc2a15aa`.

The NuGets are **unsigned Debug CI artifacts for LOCAL development and compile
validation ONLY**. A matching source commit is not signed production-release
verification. Restore used the external upgrade-workspace
`.tools\aspire17-release\validation.NuGet.config` (local `Aspire.*` source plus
approved proxy), a unique `NUGET_PACKAGES` cache
`validation-cache-squad-commerce`, and the existing global package cache only
as read-only fallback dependencies. The CLI directory was prepended to PATH
per command, and `ASPIRE_HOME` used isolated `home-squad-commerce`.
No global stable-17 cache was populated.

## Validation

The installed SDK was 10.0.404 and runtime 10.0.15. Process-scoped
DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE and DOTNET_SKIP_FIRST_TIME_EXPERIENCE
were true; NuGet's enhanced network attempt count was one.

### Aspire 17 targeted follow-up

- AppHost restore and `dotnet build --no-restore -m:1 -nr:false`: passed,
  **zero warnings/errors**, no obsolete Aspire SDK/resource API warnings.
- Windows and Linux-host-property `--locked-mode` restores: passed using the
  same **development-only** AppHost lock, subsequently removed. This establishes
  local graph portability only, not signed-release lock validity or Linux runtime behavior.
- Focused `SquadCommerce.Web.Tests` restore/build/test: **178 passed**, zero
  failures. This rebuilt Web, API and their libraries in the isolated artifacts
  tree. Three existing bUnit `TestContext` obsolete warnings and one existing
  unused-local warning remain; they were not suppressed or changed.
- Original tracked bin/obj (5,537 files) remained unchanged. Generated-only
  `.validation` output left by the interrupted worker and the follow-up's
  `.validation-aspire17` output were removed after checks.
- No npm acquisition, service/AppHost launch, browser install/test, container,
  workload, publish, deployment or CI run.

For a repeat of the local-only checks, provision and verify the external inputs
above first; use process-scoped PATH/ASPIRE_HOME/NUGET_PACKAGES, select the
external validation config explicitly, restore before `--no-restore` build/test,
and keep all commerce output outside tracked bin/obj. The isolated-build props
must be supplied to both restore and build/test. Do not reuse the global cache
for the unsigned `Aspire.*` packages.

### Earlier dependency-refresh validation (before the Aspire 17 migration)

The repository tracks old bin/obj output. A first relocated build exposed those
old generated assembly attributes as compile input; it did not modify them.
`.config\isolated-build.props` excludes that source input when explicitly selected,
so validation can relocate all new artifacts without cleaning tracked output:

```powershell
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'
$env:NUGET_ENHANCED_MAX_NETWORK_TRY_COUNT = '1'
$props = Join-Path $PWD '.config\isolated-build.props'
dotnet build SquadCommerce.slnx --artifacts-path .validation -m:1 /nodeReuse:false -p:UseSharedCompilation=false "-p:DirectoryBuildPropsPath=$props"
dotnet test SquadCommerce.slnx --no-build --no-restore --artifacts-path .validation -m:1 /nodeReuse:false --filter 'FullyQualifiedName!~Playwright'
dotnet tool restore
dotnet ef --version
```

- Solution build: passed. ASPIRE010 warns that the held 13.6.5 AppHost is not
  using the CLI bundle; it is not suppressed.
- Non-browser tests: **524 passed**, zero failures (24 A2A, 181 Agents, 41
  Integration, 100 Mcp, 178 Web).
- Playwright project: compiled against its updated dependencies; browser tests
  excluded. No browsers were installed.
- Local tool restore and `dotnet ef --version`: passed, 10.0.15.
- After correcting the Fluent UI module URL, the targeted Web.Tests build passed
  with zero warnings/errors and its **178 tests passed again**. Package asset
  inspection verified that the referenced module exists.
- Workflow YAML, SDK/tool JSON and isolated-build XML parsed successfully;
  `git diff --check` passed.
- No workload install, AppHost/server launch, container, deployment or CI run.
- Isolated validation artifacts were removed afterward; tracked bin/obj and .vs
  files remained unchanged.

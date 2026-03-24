using Microsoft.Playwright;
using NUnit.Framework;

namespace SquadCommerce.Playwright.Tests.Fixtures;

/// <summary>
/// Base test class that provides Playwright browser and page setup
/// </summary>
[TestFixture]
public abstract class PlaywrightTestBase
{
    protected IPlaywright? Playwright { get; private set; }
    protected IBrowser? Browser { get; private set; }
    protected IBrowserContext? Context { get; private set; }
    protected IPage? Page { get; private set; }
    
    protected string BaseUrl { get; private set; } = "https://localhost:7000";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Install Playwright if needed
        Program.Main(new[] { "install" });
        
        // Initialize Playwright
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        // Launch browser (headless by default, can be overridden by environment variable)
        var headless = !bool.TryParse(Environment.GetEnvironmentVariable("HEADED"), out var headed) || !headed;
        
        Browser = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = headless,
            SlowMo = 0
        });
        
        // Override base URL from environment if set
        BaseUrl = Environment.GetEnvironmentVariable("TEST_WEB_URL") ?? BaseUrl;
    }

    [SetUp]
    public async Task SetUp()
    {
        // Create a new context for each test (isolation)
        Context = await Browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true, // For local development with self-signed certs
            RecordVideoDir = GetVideoDirectory()
        });
        
        // Enable tracing for debugging
        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        
        // Create a new page
        Page = await Context.NewPageAsync();
        
        // Set default timeout
        Page.SetDefaultTimeout(30000);
    }

    [TearDown]
    public async Task TearDown()
    {
        // Take screenshot on failure
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            var screenshotPath = GetScreenshotPath();
            await Page!.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
            Console.WriteLine($"Screenshot saved to: {screenshotPath}");
            
            // Save trace on failure
            var tracePath = GetTracePath();
            await Context!.Tracing.StopAsync(new() { Path = tracePath });
            Console.WriteLine($"Trace saved to: {tracePath}");
        }
        else
        {
            // Just stop tracing without saving
            await Context!.Tracing.StopAsync();
        }
        
        await Page!.CloseAsync();
        await Context!.CloseAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await Browser!.CloseAsync();
        Playwright?.Dispose();
    }

    private string GetScreenshotPath()
    {
        var testName = TestContext.CurrentContext.Test.Name;
        var sanitizedName = string.Join("_", testName.Split(Path.GetInvalidFileNameChars()));
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        var directory = Path.Combine(GetTestOutputDirectory(), "screenshots");
        Directory.CreateDirectory(directory);
        
        return Path.Combine(directory, $"{sanitizedName}_{timestamp}.png");
    }

    private string GetTracePath()
    {
        var testName = TestContext.CurrentContext.Test.Name;
        var sanitizedName = string.Join("_", testName.Split(Path.GetInvalidFileNameChars()));
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        var directory = Path.Combine(GetTestOutputDirectory(), "traces");
        Directory.CreateDirectory(directory);
        
        return Path.Combine(directory, $"{sanitizedName}_{timestamp}.zip");
    }

    private string? GetVideoDirectory()
    {
        // Only record video on failure in CI
        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            var directory = Path.Combine(GetTestOutputDirectory(), "videos");
            Directory.CreateDirectory(directory);
            return directory;
        }
        
        return null;
    }

    private string GetTestOutputDirectory()
    {
        // Use test output directory or current directory
        var baseDir = TestContext.CurrentContext.TestDirectory ?? Directory.GetCurrentDirectory();
        return Path.Combine(baseDir, "test-results");
    }
}

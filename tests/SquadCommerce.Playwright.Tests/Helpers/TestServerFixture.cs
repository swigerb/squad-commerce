using System.Diagnostics;

namespace SquadCommerce.Playwright.Tests.Helpers;

/// <summary>
/// Manages starting and stopping the application server for E2E tests
/// </summary>
public class TestServerFixture : IDisposable
{
    private Process? _webProcess;
    private Process? _apiProcess;
    private bool _disposed;

    public string WebBaseUrl { get; private set; } = "https://localhost:7000";
    public string ApiBaseUrl { get; private set; } = "https://localhost:7001";

    public TestServerFixture()
    {
        // Override with environment variables if set
        WebBaseUrl = Environment.GetEnvironmentVariable("TEST_WEB_URL") ?? WebBaseUrl;
        ApiBaseUrl = Environment.GetEnvironmentVariable("TEST_API_URL") ?? ApiBaseUrl;
    }

    /// <summary>
    /// Starts the Web and API servers if not already running
    /// </summary>
    public async Task StartServersAsync()
    {
        // Check if servers are already running by attempting connection
        if (await IsServerRunningAsync(WebBaseUrl))
        {
            Console.WriteLine($"Web server already running at {WebBaseUrl}");
            return;
        }

        Console.WriteLine("Starting application servers...");
        
        // Option 1: Start via dotnet run (simple approach)
        // In production, you'd use Aspire AppHost or WebApplicationFactory
        
        try
        {
            // Start API project
            _apiProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run --project src/SquadCommerce.Api/SquadCommerce.Api.csproj --no-build --urls " + ApiBaseUrl,
                    WorkingDirectory = GetSolutionRoot(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            _apiProcess.Start();
            
            // Start Web project
            _webProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run --project src/SquadCommerce.Web/SquadCommerce.Web.csproj --no-build --urls " + WebBaseUrl,
                    WorkingDirectory = GetSolutionRoot(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            _webProcess.Start();
            
            // Wait for servers to be ready
            await WaitForServerReadyAsync(WebBaseUrl, timeoutSeconds: 60);
            await WaitForServerReadyAsync(ApiBaseUrl, timeoutSeconds: 60);
            
            Console.WriteLine("Servers started successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start servers: {ex.Message}");
            Console.WriteLine("Tests will attempt to connect to externally-running servers");
        }
    }

    private async Task<bool> IsServerRunningAsync(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(2);
            var response = await httpClient.GetAsync(url);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
        }
        catch
        {
            return false;
        }
    }

    private async Task WaitForServerReadyAsync(string url, int timeoutSeconds = 60)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
        {
            if (await IsServerRunningAsync(url))
            {
                Console.WriteLine($"Server ready at {url}");
                return;
            }
            
            await Task.Delay(1000);
        }
        
        throw new TimeoutException($"Server at {url} did not become ready within {timeoutSeconds} seconds");
    }

    private string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        
        // Walk up to find solution root
        while (!File.Exists(Path.Combine(currentDir, "SquadCommerce.slnx")))
        {
            var parent = Directory.GetParent(currentDir);
            if (parent == null)
            {
                throw new InvalidOperationException("Could not find solution root");
            }
            currentDir = parent.FullName;
        }
        
        return currentDir;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        Console.WriteLine("Stopping test servers...");
        
        try
        {
            _webProcess?.Kill(entireProcessTree: true);
            _webProcess?.Dispose();
        }
        catch { /* Best effort */ }
        
        try
        {
            _apiProcess?.Kill(entireProcessTree: true);
            _apiProcess?.Dispose();
        }
        catch { /* Best effort */ }
        
        _disposed = true;
        Console.WriteLine("Test servers stopped");
    }
}

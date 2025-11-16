using System.Diagnostics;
using System.Reflection;
using Microsoft.Playwright;

namespace aspire_playwright_showcase.Tests;

public class WebTests : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _webAppUrl;
    private readonly ITestOutputHelper _output;
    private Process? _appHostProcess;
    private Process? _playwrightServerProcess;

    public WebTests(ITestOutputHelper output)
    {
        _output = output;
        Environment.SetEnvironmentVariable("PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD", "1");
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", "0");
        Environment.SetEnvironmentVariable("PLAYWRIGHT_SKIP_VALIDATE_HOST_REQUIREMENTS", "1");
        StartAppHost();
        StartPlaywrightServer();
    }

    private void StartAppHost()
    {
        try
        {
            _output.WriteLine("Starting AppHost...");
        
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project ../../../../aspire-playwright-showcase.AppHost",
                WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            _appHostProcess = Process.Start(processStartInfo);

            if (_appHostProcess != null)
            {
                _output.WriteLine($"✅ AppHost process started with PID: {_appHostProcess.Id}");
            
                // Start background tasks to read output streams
                Task.Run(() => ReadOutputStream(_appHostProcess.StandardOutput, "STDOUT"));
                Task.Run(() => ReadOutputStream(_appHostProcess.StandardError, "STDERR"));
            
                // Wait for AppHost to be ready
                Thread.Sleep(15000);
            
                _output.WriteLine("AppHost startup wait completed");
            }
            else
            {
                _output.WriteLine("❌ Failed to start AppHost process");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Error starting AppHost: {ex.Message}");
        }
    }

    private async void ReadOutputStream(StreamReader reader, string streamName)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                _output.WriteLine($"[AppHost-{streamName}] {line}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Error reading {streamName}: {ex.Message}");
        }
    }

    private void StartPlaywrightServer()
    {
        try
        {
            _output.WriteLine("Starting Playwright server container...");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "podman",
                Arguments = "run -d --name playwright-server --network host mcr.microsoft.com/playwright:v1.56.0-noble /bin/sh -c \"npx -y playwright@1.56.0 run-server --port 3000 --host 0.0.0.0\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            _playwrightServerProcess = Process.Start(processStartInfo);

            if (_playwrightServerProcess != null)
            {
                // Wait a moment for the server to start up
                Thread.Sleep(5000);
            }
            else
            {
                _output.WriteLine("❌ Failed to start Playwright server process");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Error starting Playwright server: {ex.Message}");
        }
    }

     private async Task StopPlaywrightServer()
    {
        if (_playwrightServerProcess != null)
        {
            try
            {
                _output.WriteLine("Stopping Playwright server container...");

                // Stop and remove the container
                var stopProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "podman",
                        Arguments = $"stop playwright-server",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                stopProcess.Start();
                await stopProcess.WaitForExitAsync();

                var removeProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "podman",
                        Arguments = $"rm playwright-server",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                removeProcess.Start();
                await removeProcess.WaitForExitAsync();

                _output.WriteLine("✅ Playwright server container stopped and removed");

                _playwrightServerProcess.Dispose();
                _playwrightServerProcess = null;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Error stopping Playwright server: {ex.Message}");
            }
        }
    }

    public async ValueTask InitializeAsync()
    {
        _output.WriteLine("=== InitializeAsync Start ===");

        var serviceUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_URL") ?? "http://localhost:3000";
        _output.WriteLine($"Playwright Service URL: {serviceUrl}");

        // Connect to Playwright server
        _output.WriteLine("Creating Playwright connection...");
        _playwright = await Playwright.CreateAsync();
        _output.WriteLine("✅ Playwright connection created");

        _output.WriteLine($"Launching browser... ({serviceUrl})");
        _browser = await _playwright.Chromium.ConnectAsync(serviceUrl);
        _output.WriteLine("✅ Browser connected");

        // Get the URL for the web app under test
        _webAppUrl = "http://localhost:5297";
        _output.WriteLine($"✅ Web app URL: {_webAppUrl}");

        _output.WriteLine("=== InitializeAsync Complete ===");
    }


    // This runs after all tests in the class are finished
    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }
        _playwright?.Dispose();

        // Clean up the Playwright server container
        await StopPlaywrightServer();
    }

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Use the test results directory if available, otherwise use current directory
        var testResultsDir = Environment.GetEnvironmentVariable("VSTest_ResultsDirectory")
                             ?? Path.Combine(Environment.CurrentDirectory, "TestResults");
        var videoDir = Path.Combine(testResultsDir, "videos");
        Directory.CreateDirectory(videoDir);

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            RecordVideoDir = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 800 }
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(_webAppUrl);
        await page.GetByTestId("home").ClickAsync();
        await page.GetByTestId("counter").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.GetByTestId("clickme").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.GetByTestId("weather").ClickAsync();
        await page.WaitForTimeoutAsync(3000);
        await context.CloseAsync();
        // Download video from remote Playwright server
        var video = page.Video;
        await video.SaveAsAsync(Path.Combine(videoDir, "e2e-video.webm"));
        await video.DeleteAsync();
    }
}

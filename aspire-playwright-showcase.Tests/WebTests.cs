using Microsoft.Playwright;

namespace aspire_playwright_showcase.Tests;

public class WebTests : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _webAppUrl;
    private readonly ITestOutputHelper _output;

    public WebTests(ITestOutputHelper output)
    {
        _output = output;
        Environment.SetEnvironmentVariable("PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD", "1");
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", "0");
        Environment.SetEnvironmentVariable("PLAYWRIGHT_SKIP_VALIDATE_HOST_REQUIREMENTS", "1");
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

        _output.WriteLine("Launching browser...");
        _browser = await _playwright.Chromium.ConnectAsync(serviceUrl);
        _output.WriteLine("✅ Browser launched");

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
            RecordVideoDir = videoDir,
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 800 }
        });

        try
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(_webAppUrl);
            await page.GetByTestId("home").ClickAsync();
            await page.GetByTestId("counter").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.GetByTestId("clickme").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.GetByTestId("weather").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally
        {
            await context.CloseAsync();

            // Rename the generated video file to the desired name
            var videoFiles = Directory.GetFiles(videoDir, "*.webm");
            if (videoFiles.Length > 0)
            {
                var latestVideo = videoFiles.OrderByDescending(System.IO.File.GetCreationTime).First();
                var targetPath = Path.Combine(videoDir, "e2e-tests.webm");
                if (System.IO.File.Exists(targetPath))
                    System.IO.File.Delete(targetPath);
                System.IO.File.Move(latestVideo, targetPath);
            }
        }
    }
}

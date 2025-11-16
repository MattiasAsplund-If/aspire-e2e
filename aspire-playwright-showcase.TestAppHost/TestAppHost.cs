var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.aspire_playwright_showcase_Web>("webfrontend")
    .WithHttpEndpoint(port: 0);

builder.Build().Run();

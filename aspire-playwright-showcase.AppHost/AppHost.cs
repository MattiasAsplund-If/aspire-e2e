using k8s.Models;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.aspire_playwright_showcase_ApiService>("apiservice");

builder.AddProject<Projects.aspire_playwright_showcase_Web>("webfrontend")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

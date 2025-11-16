# aspire-e2e

Imagine cloning an Aspire repo and running an end-to-end test within seconds.

That is exactly what this does.

You will to have [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed though.

That is a dependency of Aspire 13, the newly released version with support for both .NET and Python/Vite. Read more at [What's new in Aspire 13](https://aspire.dev/whats-new/aspire-13/).

# Run the test

With .NET SDK 10.0 installed, go into **aspire-playwright-showcase.Tests** and write: **dotnet test**. Wait for the test to end and then verify the results by viewing the recorded video in **aspire-playwright-showcase.Tests/bin/Debug/net10.0/TestResults/videos**.

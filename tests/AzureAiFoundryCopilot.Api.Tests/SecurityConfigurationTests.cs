using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class SecurityConfigurationTests
{
    [Fact]
    public void Startup_WhenProductionAndEntraIdDisabled_Throws()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["EntraId:Enabled"] = "false"
                    });
                });
            });

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }
}

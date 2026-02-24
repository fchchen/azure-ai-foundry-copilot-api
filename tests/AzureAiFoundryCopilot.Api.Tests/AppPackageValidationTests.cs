using System.Text.Json;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class AppPackageValidationTests
{
    private static readonly string AppPackagePath = Path.Combine(AppContext.BaseDirectory, "appPackage");

    private static JsonElement LoadJson(string fileName)
    {
        var path = Path.Combine(AppPackagePath, fileName);
        Assert.True(File.Exists(path), $"{fileName} should exist in appPackage output directory.");
        var content = File.ReadAllText(path);
        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    [Theory]
    [InlineData("manifest.json")]
    [InlineData("declarativeAgent.json")]
    [InlineData("ai-plugin.json")]
    [InlineData("openapi.json")]
    public void StaticFile_IsWellFormedJson(string fileName)
    {
        var json = LoadJson(fileName);
        Assert.NotEqual(JsonValueKind.Undefined, json.ValueKind);
    }

    [Fact]
    public void ManifestChain_ManifestReferencesDeclarativeAgent()
    {
        var manifest = LoadJson("manifest.json");
        var agents = manifest.GetProperty("copilotAgents").GetProperty("declarativeAgents");
        var file = agents[0].GetProperty("file").GetString();
        Assert.Equal("declarativeAgent.json", file);
    }

    [Fact]
    public void ManifestChain_DeclarativeAgentReferencesAiPlugin()
    {
        var da = LoadJson("declarativeAgent.json");
        var file = da.GetProperty("actions")[0].GetProperty("file").GetString();
        Assert.Equal("ai-plugin.json", file);
    }

    [Fact]
    public void ManifestChain_AiPluginReferencesOpenApi()
    {
        var plugin = LoadJson("ai-plugin.json");
        var specUrl = plugin.GetProperty("runtimes")[0].GetProperty("spec").GetProperty("url").GetString();
        Assert.Equal("openapi.json", specUrl);
    }

    [Fact]
    public void OperationIds_InOpenApiMatchAiPluginFunctions()
    {
        var plugin = LoadJson("ai-plugin.json");
        var openapi = LoadJson("openapi.json");

        var pluginFunctionNames = plugin.GetProperty("functions")
            .EnumerateArray()
            .Select(f => f.GetProperty("name").GetString()!)
            .OrderBy(n => n)
            .ToList();

        var openApiOperationIds = openapi.GetProperty("paths")
            .EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Select(method => method.Value.GetProperty("operationId").GetString()!)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(pluginFunctionNames, openApiOperationIds);
    }

    [Fact]
    public void AiPlugin_RuntimeRunForFunctionsMatchFunctionNames()
    {
        var plugin = LoadJson("ai-plugin.json");

        var functionNames = plugin.GetProperty("functions")
            .EnumerateArray()
            .Select(f => f.GetProperty("name").GetString()!)
            .OrderBy(n => n)
            .ToList();

        var runForFunctions = plugin.GetProperty("runtimes")[0]
            .GetProperty("run_for_functions")
            .EnumerateArray()
            .Select(f => f.GetString()!)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(functionNames, runForFunctions);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class OpenApiParityIntegrationTests
{
    [Fact]
    public async Task CopilotOpenApi_MatchesGeneratedContractsForPluginSurface()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var generatedResponse = await client.GetAsync("/openapi/v1.json");
        var copilotResponse = await client.GetAsync("/api/copilot/openapi");

        Assert.Equal(HttpStatusCode.OK, generatedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, copilotResponse.StatusCode);

        var generated = await generatedResponse.Content.ReadFromJsonAsync<JsonElement>();
        var copilot = await copilotResponse.Content.ReadFromJsonAsync<JsonElement>();

        var generatedOperations = CollectOperations(generated);
        var copilotOperations = CollectOperations(copilot);

        var missingOperations = copilotOperations
            .Where(operation => !generatedOperations.Contains(operation))
            .OrderBy(operation => operation)
            .ToList();

        Assert.True(
            missingOperations.Count == 0,
            $"The generated OpenAPI is missing Copilot operations: {string.Join(", ", missingOperations)}");

        var copilotSchemas = GetSchemas(copilot);
        var generatedSchemas = GetSchemas(generated);

        foreach (var schema in copilotSchemas)
        {
            Assert.True(
                generatedSchemas.ContainsKey(schema.Key),
                $"Generated OpenAPI is missing schema '{schema.Key}'.");

            var staticSchema = schema.Value;
            var generatedSchema = generatedSchemas[schema.Key];

            var staticProperties = GetPropertyNames(staticSchema);
            var generatedProperties = GetPropertyNames(generatedSchema);
            Assert.Equal(staticProperties, generatedProperties);

            var staticRequired = GetRequiredProperties(staticSchema);
            var generatedRequired = GetRequiredProperties(generatedSchema);
            Assert.Equal(staticRequired, generatedRequired);
        }
    }

    private static HashSet<string> CollectOperations(JsonElement openApi)
    {
        var operations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!openApi.TryGetProperty("paths", out var paths))
            return operations;

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                var key = $"{method.Name.ToUpperInvariant()} {path.Name}";
                operations.Add(key);
            }
        }

        return operations;
    }

    private static Dictionary<string, JsonElement> GetSchemas(JsonElement openApi)
    {
        var schemas = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (!openApi.TryGetProperty("components", out var components) ||
            !components.TryGetProperty("schemas", out var openApiSchemas))
        {
            return schemas;
        }

        foreach (var schema in openApiSchemas.EnumerateObject())
        {
            schemas[schema.Name] = schema.Value;
        }

        return schemas;
    }

    private static IReadOnlyList<string> GetPropertyNames(JsonElement schema)
    {
        if (!schema.TryGetProperty("properties", out var properties))
            return [];

        return properties
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<string> GetRequiredProperties(JsonElement schema)
    {
        if (!schema.TryGetProperty("required", out var required))
            return [];

        return required
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }
}

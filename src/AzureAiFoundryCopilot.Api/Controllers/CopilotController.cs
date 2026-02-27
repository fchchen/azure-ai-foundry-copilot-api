using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using AzureAiFoundryCopilot.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AzureAiFoundryCopilot.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/copilot")]
public sealed class CopilotController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, CachedJsonContent> JsonCache = new(StringComparer.Ordinal);

    private readonly ICopilotManifestService _copilotManifestService;
    private readonly CopilotPluginOptions _copilotOptions;
    private readonly EntraIdOptions _entraIdOptions;

    public CopilotController(
        ICopilotManifestService copilotManifestService,
        IOptions<CopilotPluginOptions> copilotOptions,
        IOptions<EntraIdOptions> entraIdOptions)
    {
        _copilotManifestService = copilotManifestService;
        _copilotOptions = copilotOptions.Value;
        _entraIdOptions = entraIdOptions.Value;
    }

    [HttpGet("manifest")]
    [ProducesResponseType(typeof(CopilotManifestResponse), StatusCodes.Status200OK)]
    public IActionResult GetManifest() => Ok(_copilotManifestService.GetManifest());

    [HttpGet("openapi")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetOpenApiSpec()
    {
        var cacheKeySuffix = $"{_copilotOptions.ApiBaseUrl}|{ResolveTenantSegment()}|{ResolveApiScope()}";
        return ServeCachedJson("openapi.json", ApplyOpenApiRuntimeValues, cacheKeySuffix);
    }

    [HttpGet("ai-plugin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAiPlugin()
    {
        var cacheKeySuffix = $"{_copilotOptions.ApiBaseUrl}|{ResolveTenantSegment()}|{ResolveApiScope()}";
        return ServeCachedJson("ai-plugin.json", ApplyAiPluginRuntimeValues, cacheKeySuffix);
    }

    private void ApplyOpenApiRuntimeValues(JsonNode root)
    {
        var tenantSegment = ResolveTenantSegment();
        var scope = ResolveApiScope();
        var authUrl = $"https://login.microsoftonline.com/{tenantSegment}/oauth2/v2.0/authorize";
        var tokenUrl = $"https://login.microsoftonline.com/{tenantSegment}/oauth2/v2.0/token";

        if (root["servers"] is JsonArray servers &&
            servers.Count > 0 &&
            servers[0] is JsonObject firstServer &&
            Uri.TryCreate(_copilotOptions.ApiBaseUrl, UriKind.Absolute, out _))
        {
            firstServer["url"] = _copilotOptions.ApiBaseUrl.TrimEnd('/');
        }

        if (root["security"] is JsonArray security &&
            security.Count > 0 &&
            security[0] is JsonObject firstSecurityObject)
        {
            firstSecurityObject["oauth2Auth"] = new JsonArray(scope);
        }

        if (root["components"]?["securitySchemes"]?["oauth2Auth"]?["flows"]?["authorizationCode"] is JsonObject authCode)
        {
            authCode["authorizationUrl"] = authUrl;
            authCode["tokenUrl"] = tokenUrl;

            if (authCode["scopes"] is JsonObject scopes)
            {
                scopes.Clear();
                scopes[scope] = "Access Azure AI Foundry Copilot API as signed-in user.";
            }
        }
    }

    private void ApplyAiPluginRuntimeValues(JsonNode root)
    {
        var tenantSegment = ResolveTenantSegment();
        var scope = ResolveApiScope();
        var authUrl = $"https://login.microsoftonline.com/{tenantSegment}/oauth2/v2.0/authorize";
        var tokenUrl = $"https://login.microsoftonline.com/{tenantSegment}/oauth2/v2.0/token";

        if (root["runtimes"] is not JsonArray runtimes || runtimes.Count == 0 || runtimes[0] is not JsonObject runtime)
            return;

        if (runtime["auth"] is not JsonObject auth)
        {
            auth = new JsonObject();
            runtime["auth"] = auth;
        }

        auth["type"] = "OAuth";
        auth["authorization_url"] = authUrl;
        auth["token_url"] = tokenUrl;
        auth["scope"] = scope;
    }

    private string ResolveTenantSegment()
    {
        if (IsPlaceholder(_entraIdOptions.TenantId))
            return "common";

        return _entraIdOptions.TenantId.Trim();
    }

    private string ResolveApiScope()
    {
        var audience = _entraIdOptions.Audience;
        if (IsPlaceholder(audience))
        {
            audience = IsPlaceholder(_entraIdOptions.ClientId)
                ? "api://YOUR-CLIENT-ID"
                : $"api://{_entraIdOptions.ClientId.Trim()}";
        }

        if (audience.EndsWith("/access_as_user", StringComparison.OrdinalIgnoreCase))
            return audience;

        return $"{audience.TrimEnd('/')}/access_as_user";
    }

    private static bool IsPlaceholder(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Contains("YOUR-", StringComparison.OrdinalIgnoreCase);

    private IActionResult ServeCachedJson(string fileName, Action<JsonNode> applyRuntimeValues, string cacheKeySuffix)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appPackage", fileName);
        if (!System.IO.File.Exists(path))
            return NotFound();

        var fileLastWrite = System.IO.File.GetLastWriteTimeUtc(path);
        var cacheKey = $"{path}|{cacheKeySuffix}";

        var content = JsonCache.AddOrUpdate(
            cacheKey,
            _ => BuildCachedJson(path, fileLastWrite, applyRuntimeValues),
            (_, existing) =>
                existing.FileLastWriteUtc == fileLastWrite
                    ? existing
                    : BuildCachedJson(path, fileLastWrite, applyRuntimeValues));

        if (MatchesIfNoneMatchHeader(content.ETag))
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = content.ETag;
        Response.Headers.CacheControl = "public, max-age=60";
        return Content(content.Content, "application/json");
    }

    private static CachedJsonContent BuildCachedJson(
        string path,
        DateTime fileLastWrite,
        Action<JsonNode> applyRuntimeValues)
    {
        var root = JsonNode.Parse(System.IO.File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Could not parse JSON file: {path}");
        applyRuntimeValues(root);

        var content = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var eTag = $"\"{Convert.ToHexString(hashBytes[..16])}\"";

        return new CachedJsonContent(fileLastWrite, content, eTag);
    }

    private bool MatchesIfNoneMatchHeader(string eTag)
    {
        if (!Request.Headers.TryGetValue("If-None-Match", out var headerValues))
            return false;

        return headerValues
            .SelectMany(value => (value ?? string.Empty)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Any(tag => string.Equals(tag, eTag, StringComparison.Ordinal));
    }

    private sealed record CachedJsonContent(
        DateTime FileLastWriteUtc,
        string Content,
        string ETag);
}

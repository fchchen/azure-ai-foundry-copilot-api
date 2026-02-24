namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class AppInsightsOptions
{
    public const string SectionName = "ApplicationInsights";

    public bool Enabled { get; init; }

    public string ConnectionString { get; init; } = string.Empty;
}

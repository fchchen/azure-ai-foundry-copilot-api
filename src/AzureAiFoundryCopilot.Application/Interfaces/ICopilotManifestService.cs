using AzureAiFoundryCopilot.Application.Contracts;

namespace AzureAiFoundryCopilot.Application.Interfaces;

public interface ICopilotManifestService
{
    CopilotManifestResponse GetManifest();
}

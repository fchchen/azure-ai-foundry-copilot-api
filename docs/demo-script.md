# 7-Minute Demo Script (AI Foundry + M365 Copilot)

## Goal
Show practical, enterprise-ready experience with:
- Azure AI Foundry integration in a .NET Web API
- Microsoft 365 Copilot plugin/declarative agent integration

## Setup (before demo)
1. Start API:
```bash
dotnet run --project src/AzureAiFoundryCopilot.Api
```
2. Keep `UseMockResponses=true` for deterministic demo unless showing live Azure.
3. Open:
- `http://localhost:5153/scalar/v1`
- `http://localhost:5153/api/copilot/manifest`

## Minute 0-1: Architecture Snapshot
Say:
- "This is a layered .NET API with Application orchestration and Infrastructure adapters."
- "The core AI flows are not in controllers; orchestration lives in Application services."

Show:
- `src/AzureAiFoundryCopilot.Application/Services/AiFoundryOrchestrationService.cs`
- `src/AzureAiFoundryCopilot.Api/Controllers/AiFoundryController.cs`

## Minute 1-3: AI Foundry Skill
Call:
- `POST /api/ai-foundry/chat`
- `POST /api/ai-foundry/unread-email-summary`

Say:
- "Chat completion is routed through AI Foundry service abstraction."
- "Unread inbox summary composes Graph data + AI summarization for prioritized actions."

Show:
- `src/AzureAiFoundryCopilot.Infrastructure/Services/AiFoundryChatService.cs`
- `src/AzureAiFoundryCopilot.Infrastructure/Services/GraphMailService.cs`

## Minute 3-5: M365 Copilot Integration
Call:
- `GET /api/copilot/manifest`
- `GET /api/copilot/openapi`
- `GET /api/copilot/ai-plugin`

Say:
- "The plugin chain is complete: manifest -> declarativeAgent -> ai-plugin -> OpenAPI."
- "OAuth metadata is runtime-configured from Entra settings."
- "Endpoints support ETag/304 for efficient plugin metadata delivery."

Show:
- `appPackage/manifest.json`
- `appPackage/declarativeAgent.json`
- `appPackage/ai-plugin.json`
- `appPackage/openapi.json`
- `src/AzureAiFoundryCopilot.Api/Controllers/CopilotController.cs`

## Minute 5-6: Security + Operations
Say:
- "Mock auth is blocked outside Development/Testing."
- "CORS is deny-by-default when origins are not configured."
- "Health endpoint only returns detailed infra flags in Development."
- "Coverage is published in CI artifacts and job summary."

Show:
- `src/AzureAiFoundryCopilot.Api/DependencyInjection/ServiceCollectionExtensions.cs`
- `src/AzureAiFoundryCopilot.Api/Program.cs`
- `.github/workflows/dotnet-ci.yml`

## Minute 6-7: Test Evidence
Run:
```bash
dotnet test azure-ai-foundry-copilot-api.slnx
```

Say:
- "Tests include manifest-chain validation, OpenAPI parity, auth/security behavior, orchestration unit tests, and integration coverage."
- "This demonstrates both implementation and production-hardening discipline."

## Optional live-environment note
- Use `scripts/configure-apppackage-oauth.sh` before uploading package artifacts.
- Switch to real Azure/Graph by setting `UseMockResponses=false` + Entra/Graph settings.

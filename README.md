# azure-ai-foundry-copilot-api

Enterprise .NET 10 API for Azure AI Foundry chat completions with a real Microsoft 365 Copilot declarative agent plugin — clean architecture, mock mode, embedded chat UI, and Scalar API docs.

## Screenshots

### Copilot Plugin Manifest Chain

![Copilot plugin manifest chain](docs/screenshots/copilot-plugin-chain.png)

### Copilot Manifest Endpoint

![Copilot manifest response](docs/screenshots/copilot-manifest.png)

### Embedded Chat UI

![Chat UI — conversation](docs/screenshots/chat-ui-conversation.png)

### Scalar API Reference

![Scalar API docs](docs/screenshots/scalar-api-docs.png)

### Health Endpoint

![Health endpoint JSON](docs/screenshots/health-endpoint.png)

## Tech Stack

- C# / .NET 10 Web API (compatible with .NET 6+ concepts)
- Layered architecture (`Api`, `Application`, `Infrastructure`, `Tests`)
- REST endpoints with OpenAPI
- Azure AI Foundry integration path (mock mode by default)
- **M365 Copilot declarative agent** — full `appPackage/` with manifest chain, API plugin, Adaptive Cards
- Copilot metadata endpoints (`manifest`, `openapi`, `ai-plugin`)
- Embedded chat UI (vanilla HTML/CSS/JS)
- Scalar API reference (interactive docs)
- xUnit tests (unit + integration + manifest validation)
- GitHub Actions CI

## M365 Copilot Plugin

The `appPackage/` directory contains a complete declarative agent plugin that follows the Microsoft 365 Copilot manifest chain:

```
manifest.json → declarativeAgent.json → ai-plugin.json → openapi.json
```

**Plugin functions** (mapped to API endpoints):

| Function | Method | Path |
|----------|--------|------|
| `sendChatPrompt` | `POST` | `/api/ai-foundry/chat` |
| `summarizeMyUnreadEmails` | `POST` | `/api/ai-foundry/unread-email-summary` |
| `getConversation` | `GET` | `/api/conversations/{conversationId}` |
| `listRecentConversations` | `GET` | `/api/conversations/recent` |
| `saveConversation` | `POST` | `/api/conversations` |

Each function includes Adaptive Card v1.5 response templates for rich rendering in Copilot.
Runtime auth is configured as OAuth in `ai-plugin.json` and OpenAPI security schemes.

## Solution Structure

- `appPackage/`: M365 Copilot plugin manifests (Teams app, declarative agent, API plugin, OpenAPI 3.0.1 spec)
- `src/AzureAiFoundryCopilot.Api`: HTTP API, DI wiring, embedded chat UI
- `src/AzureAiFoundryCopilot.Application`: Contracts, use-case orchestration services, and service interfaces
- `src/AzureAiFoundryCopilot.Infrastructure`: AI Foundry + Copilot service implementations
- `tests/AzureAiFoundryCopilot.Api.Tests`: Unit, integration, and manifest validation tests

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/` | Embedded chat UI |
| `GET` | `/scalar/v1` | Interactive API reference (dev) |
| `GET` | `/openapi/v1.json` | OpenAPI document (dev) |
| `GET` | `/api/health` | Health and service metadata |
| `POST` | `/api/ai-foundry/chat` | AI prompt completion |
| `POST` | `/api/ai-foundry/unread-email-summary` | Summarize unread Microsoft 365 inbox email with AI |
| `GET` | `/api/conversations/{id}` | Get conversation by ID |
| `GET` | `/api/conversations/recent` | List recent conversations |
| `POST` | `/api/conversations` | Save a conversation |
| `GET` | `/api/copilot/manifest` | Copilot plugin metadata |
| `GET` | `/api/copilot/openapi` | OpenAPI 3.0.1 spec (Copilot-compatible) |
| `GET` | `/api/copilot/ai-plugin` | AI plugin definition |

## Local Run

```bash
dotnet restore
dotnet build azure-ai-foundry-copilot-api.slnx
dotnet run --project src/AzureAiFoundryCopilot.Api
```

Then open:
- **Chat UI**: http://localhost:5153/
- **API docs**: http://localhost:5153/scalar/v1

## Configuration

`src/AzureAiFoundryCopilot.Api/appsettings.json` includes:

- `AzureAiFoundry`: endpoint, deployment, api key or key-vault secret name, mock mode
- `MicrosoftGraph`: Graph API base URL and unread inbox path (real Graph mode toggle)
- `EntraId`: required for production-like environments (mock auth allowed only in Development/Testing)
- `CopilotPlugin`: plugin metadata, developer info, API base URL, declarative-agent instructions, and conversation starters
- `Cors`: explicit `AllowedOrigins` list (no `AllowAnyOrigin` fallback outside Development)

For local demos, keep `UseMockResponses=true`.
For real Azure calls, store secrets with [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):

```bash
cd src/AzureAiFoundryCopilot.Api
dotnet user-secrets init
dotnet user-secrets set "AzureAiFoundry:ApiKey" "<your-key>"
dotnet user-secrets set "AzureAiFoundry:ApiKeySecretName" "AzureAiFoundry--ApiKey"
dotnet user-secrets set "AzureAiFoundry:Endpoint" "https://YOUR-RESOURCE.openai.azure.com"
dotnet user-secrets set "AzureAiFoundry:UseMockResponses" "false"
dotnet user-secrets set "MicrosoftGraph:Enabled" "true"
dotnet user-secrets set "EntraId:Enabled" "true"
dotnet user-secrets set "EntraId:TenantId" "<tenant-id-guid>"
dotnet user-secrets set "EntraId:ClientId" "<api-app-registration-client-id>"
dotnet user-secrets set "EntraId:Audience" "api://<api-app-registration-client-id>"
dotnet user-secrets set "CopilotPlugin:ApiBaseUrl" "https://<your-api-hostname>"
```

For production health probes, `/api/health` returns only high-level status/service/time. Detailed infrastructure flags are limited to Development.

`/api/copilot/openapi` and `/api/copilot/ai-plugin` now render tenant/client/scope/API base URL from runtime configuration, so the metadata stays aligned with your deployed environment.
These endpoints also return `ETag` headers and support `304 Not Modified` when clients send `If-None-Match`.

## Package OAuth Configuration

Before uploading the Teams/Copilot `appPackage`, stamp OAuth values into static package files:

```bash
./scripts/configure-apppackage-oauth.sh <tenant-id> <client-id> <api-base-url>
```

This updates `appPackage/ai-plugin.json` and `appPackage/openapi.json` with tenant-specific OAuth URLs, scope, and API server URL.

## Tests

```bash
dotnet test azure-ai-foundry-copilot-api.slnx
```

CI also collects coverage (`XPlat Code Coverage`), generates a summary in the GitHub job summary, and uploads a coverage artifact.

## JD Alignment

- `.NET Web API`: controller-based REST APIs with layered design
- `Azure cloud-native`: ready for App Service/container deployment
- `AI Foundry`: configurable chat-completions integration
- `Microsoft 365 Copilot`: declarative agent plugin with full manifest chain, API plugin, and Adaptive Cards
- `DevOps`: GitHub Actions CI workflow included
- `Security`: API-key based secure outbound Foundry calls, config-driven secrets

using AzureAiFoundryCopilot.Api.DependencyInjection;
using AzureAiFoundryCopilot.Application.Interfaces;
using AzureAiFoundryCopilot.Application.Services;
using AzureAiFoundryCopilot.Infrastructure.Options;
using AzureAiFoundryCopilot.Infrastructure.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

// Options — bind + validate on startup
builder.Services
    .AddOptions<AzureAiFoundryOptions>()
    .BindConfiguration(AzureAiFoundryOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<CopilotPluginOptions>()
    .BindConfiguration(CopilotPluginOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// CORS — config-driven origins
var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
            policy.AllowAnyHeader().AllowAnyMethod();
            return;
        }

        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:4200");
            policy.AllowAnyHeader().AllowAnyMethod();
            return;
        }

        // Secure-by-default for production-like environments when origins are not configured.
        policy.SetIsOriginAllowed(_ => false);
    });
});

builder.Services.AddSingleton<IAiFoundryChatService, AiFoundryChatService>();
builder.Services.AddSingleton<IAiFoundryOrchestrationService, AiFoundryOrchestrationService>();

builder.Services.AddSingleton<ICopilotManifestService, CopilotManifestService>();

// Azure services — each opt-in via Enabled flag
builder.Services.AddKeyVaultServices(builder.Configuration);
builder.Services.AddBlobStorageServices(builder.Configuration);
builder.Services.AddGraphServices(builder.Configuration);
builder.Services.AddEntraIdAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAppInsightsTelemetry(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

public partial class Program { }

using AzureAiFoundryCopilot.Application.Interfaces;
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
    .ValidateOnStart();

builder.Services
    .AddOptions<CopilotPluginOptions>()
    .BindConfiguration(CopilotPluginOptions.SectionName)
    .ValidateOnStart();

// CORS — config-driven origins
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins);
        else
            policy.AllowAnyOrigin();

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

// HttpClient with resilience
builder.Services
    .AddHttpClient<IAiFoundryChatService, AiFoundryChatService>()
    .AddStandardResilienceHandler();

builder.Services.AddSingleton<ICopilotManifestService, CopilotManifestService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();

public partial class Program { }

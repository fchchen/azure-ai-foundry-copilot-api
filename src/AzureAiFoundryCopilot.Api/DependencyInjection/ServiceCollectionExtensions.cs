using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using AzureAiFoundryCopilot.Api.Auth;
using AzureAiFoundryCopilot.Application.Interfaces;
using AzureAiFoundryCopilot.Infrastructure.Options;
using AzureAiFoundryCopilot.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using System.Net.Http.Headers;

namespace AzureAiFoundryCopilot.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeyVaultServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KeyVaultOptions>()
            .BindConfiguration(KeyVaultOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(KeyVaultOptions.SectionName).Get<KeyVaultOptions>();

        if (options?.Enabled is true)
        {
            services.AddSingleton(new SecretClient(new Uri(options.VaultUri), new DefaultAzureCredential()));
            services.AddSingleton<ISecretService, KeyVaultSecretService>();
        }
        else
        {
            services.AddSingleton<ISecretService, InMemorySecretService>();
        }

        return services;
    }

    public static IServiceCollection AddBlobStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<BlobStorageOptions>()
            .BindConfiguration(BlobStorageOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>();

        if (options?.Enabled is true)
        {
            services.AddSingleton(new BlobContainerClient(options.ConnectionString, options.ContainerName));
            services.AddSingleton<IConversationStorageService, BlobConversationStorageService>();
        }
        else
        {
            services.AddSingleton<IConversationStorageService, InMemoryConversationStorageService>();
        }

        return services;
    }

    public static IServiceCollection AddEntraIdAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services
            .AddOptions<EntraIdOptions>()
            .BindConfiguration(EntraIdOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(EntraIdOptions.SectionName).Get<EntraIdOptions>();

        if (options?.Enabled is true)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection(EntraIdOptions.SectionName));

            services.AddAuthorization();
        }
        else if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            services.AddAuthentication(MockAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, MockAuthHandler>(
                    MockAuthHandler.SchemeName, _ => { });

            services.AddAuthorization();
        }
        else
        {
            throw new InvalidOperationException(
                "Mock authentication is only allowed in Development/Testing. Set EntraId:Enabled=true for production-like environments.");
        }

        return services;
    }

    public static IServiceCollection AddAppInsightsTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AppInsightsOptions>()
            .BindConfiguration(AppInsightsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(AppInsightsOptions.SectionName).Get<AppInsightsOptions>();

        if (options?.Enabled is true)
        {
            services.AddApplicationInsightsTelemetry(o =>
            {
                o.ConnectionString = options.ConnectionString;
            });
        }

        services.AddHealthChecks();

        return services;
    }

    public static IServiceCollection AddGraphServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MicrosoftGraphOptions>()
            .BindConfiguration(MicrosoftGraphOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(MicrosoftGraphOptions.SectionName).Get<MicrosoftGraphOptions>();

        if (options?.Enabled is true)
        {
            services.AddHttpClient<IGraphMailService, GraphMailService>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
        }
        else
        {
            services.AddSingleton<IGraphMailService, MockGraphMailService>();
        }

        return services;
    }
}

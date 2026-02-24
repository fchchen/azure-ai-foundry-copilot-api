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

namespace AzureAiFoundryCopilot.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeyVaultServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KeyVaultOptions>()
            .BindConfiguration(KeyVaultOptions.SectionName)
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

    public static IServiceCollection AddEntraIdAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<EntraIdOptions>()
            .BindConfiguration(EntraIdOptions.SectionName)
            .ValidateOnStart();

        var options = configuration.GetSection(EntraIdOptions.SectionName).Get<EntraIdOptions>();

        if (options?.Enabled is true)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection(EntraIdOptions.SectionName));

            services.AddAuthorization();
        }
        else
        {
            services.AddAuthentication(MockAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, MockAuthHandler>(
                    MockAuthHandler.SchemeName, _ => { });

            services.AddAuthorization();
        }

        return services;
    }

    public static IServiceCollection AddAppInsightsTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AppInsightsOptions>()
            .BindConfiguration(AppInsightsOptions.SectionName)
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
}

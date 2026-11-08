using System.Text.Json;
using AppTemplate.Api.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace AppTemplate.Api.Client;

/// <summary>
/// Extension methods for registering the typed <see cref="IAppTemplateApiClient"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the typed <see cref="IAppTemplateApiClient"/> backed by Refit and
    /// <see cref="IHttpClientFactory"/>, pointing at the supplied API base address.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="baseAddress">The base address of the AppTemplate API.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further configuration.</returns>
    public static IHttpClientBuilder AddAppTemplateApiClient(this IServiceCollection services, Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(baseAddress);

        JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
        {
            TypeInfoResolverChain = { ApiJsonSerializerContext.Default },
        };

        RefitSettings refitSettings = new()
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions),
        };

        return services
            .AddRefitClient<IAppTemplateApiClient>(refitSettings)
            .ConfigureHttpClient(client => client.BaseAddress = baseAddress);
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UtilityData.Application.Abstractions;
using UtilityData.Infrastructure.Ai;

namespace UtilityData.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddUtilityInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));
        services.AddHttpClient<IAiAssistantService, OllamaAiAssistant>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });
        return services;
    }
}

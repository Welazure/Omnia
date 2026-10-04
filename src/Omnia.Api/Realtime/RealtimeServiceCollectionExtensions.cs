using Microsoft.Extensions.Configuration;

namespace Omnia.Api.Realtime;

public static class RealtimeServiceCollectionExtensions
{
    public const string ClientCorsPolicy = "omnia-client";

    public static IServiceCollection AddOmniaRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();
        services.AddCors(options => options.AddPolicy(ClientCorsPolicy, policy => policy
            .WithOrigins(ResolveAllowedOrigins(configuration))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        return services;
    }

    private static string[] ResolveAllowedOrigins(IConfiguration configuration) =>
        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5000"];
}

using System;
using Microsoft.Extensions.DependencyInjection;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOmniaClient(this IServiceCollection services, Action<ApiOptions>? configure = null)
    {
        var options = new ApiOptions { BaseUrl = ResolveBaseUrl() };
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddLogging();
        services.AddSingleton<ITokenStore>(_ => new FileTokenStore());
        services.AddHttpClient<IApiClient, ApiClient>(client => client.BaseAddress = options.BaseUri);
        services.AddSingleton<IHubConnectionFactory, HubConnectionFactory>();
        services.AddSingleton<IRealtimeClient, RealtimeClient>();
        services.AddSingleton<IClipSyncService, ClipSyncService>();
        services.AddTransient<LoginViewModel>();
        services.AddSingleton<IPageFactory, PageFactory>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<INavigationService>(provider => provider.GetRequiredService<ShellViewModel>());

        return services;
    }

    private static string ResolveBaseUrl() =>
        Environment.GetEnvironmentVariable("OMNIA_API_BASE_URL") ?? ApiOptions.DefaultBaseUrl;
}

using System;
using Microsoft.Extensions.DependencyInjection;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Services;

public static class ServiceCollectionExtensions
{
    private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(2);

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
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<AvaloniaClipboardService>();
        services.AddSingleton<IClipboardService>(provider => provider.GetRequiredService<AvaloniaClipboardService>());
        services.AddSingleton<ToastDelay>(_ => cancellationToken => Task.Delay(ToastDuration, cancellationToken));
        services.AddTransient<LoginViewModel>();
        services.AddSingleton<ClipboardViewModel>();
        services.AddSingleton<AccountViewModel>();
        services.AddSingleton<IPageFactory, PageFactory>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<INavigationService>(provider => provider.GetRequiredService<ShellViewModel>());

        return services;
    }

    private static string ResolveBaseUrl() =>
        Environment.GetEnvironmentVariable("OMNIA_API_BASE_URL") ?? ApiOptions.DefaultBaseUrl;
}

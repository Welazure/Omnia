using Microsoft.Extensions.DependencyInjection;
using Omnia.Client.Services;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddOmniaClient_ResolvesAllClientServices()
    {
        await using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IApiClient>());
        Assert.NotNull(provider.GetRequiredService<ITokenStore>());
        Assert.NotNull(provider.GetRequiredService<IRealtimeClient>());
        Assert.NotNull(provider.GetRequiredService<IClipSyncService>());
        Assert.NotNull(provider.GetRequiredService<IPageFactory>());
        Assert.NotNull(provider.GetRequiredService<ShellViewModel>());
    }

    [Fact]
    public async Task AddOmniaClient_NavigationServiceIsTheShell()
    {
        await using var provider = BuildProvider();

        var shell = provider.GetRequiredService<ShellViewModel>();
        var navigation = provider.GetRequiredService<INavigationService>();

        Assert.Same(shell, navigation);
    }

    [Fact]
    public async Task AddOmniaClient_PageFactory_CreatesLoginPage()
    {
        await using var provider = BuildProvider();

        var page = provider.GetRequiredService<IPageFactory>().CreateLogin();

        Assert.IsType<LoginViewModel>(page);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddOmniaClient(options => options.BaseUrl = "http://localhost:9999");
        return services.BuildServiceProvider();
    }
}

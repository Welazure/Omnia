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
        Assert.NotNull(provider.GetRequiredService<IDeviceIdStore>());
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

    [Fact]
    public async Task AddOmniaClient_PageFactory_CreatesWorkspaceAndAccountPages()
    {
        await using var provider = BuildProvider();

        var factory = provider.GetRequiredService<IPageFactory>();

        Assert.IsType<ClipboardViewModel>(factory.CreateWorkspace());
        Assert.IsType<AccountViewModel>(factory.CreateAccount());
    }

    [Fact]
    public async Task AddOmniaClient_ResolvesWorkspaceServices()
    {
        await using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IClipboardService>());
        Assert.NotNull(provider.GetRequiredService<IUiDispatcher>());
        Assert.NotNull(provider.GetRequiredService<ToastDelay>());
        Assert.NotNull(provider.GetRequiredService<ClipboardViewModel>());
        Assert.NotNull(provider.GetRequiredService<AccountViewModel>());
    }

    [Fact]
    public async Task AddOmniaClient_RegistersClipboardCapability()
    {
        await using var provider = BuildProvider();

        Assert.Equal(ClipboardCapability.Supported, provider.GetRequiredService<ClipboardCapability>());
    }

    [Fact]
    public async Task AddOmniaClient_ResolvesClipboardMonitorServices()
    {
        await using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IClipboardMonitor>());
        Assert.NotNull(provider.GetRequiredService<IClipboardTickSource>());
        Assert.NotNull(provider.GetRequiredService<ClipboardWriteGuard>());
    }

    [Fact]
    public async Task AddOmniaClient_ResolvesAutoSyncServices()
    {
        await using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IAutoSyncSettings>());
        Assert.NotNull(provider.GetRequiredService<ClipboardSyncCoordinator>());
        Assert.NotNull(provider.GetRequiredService<RemoteClipApplier>());
        Assert.NotNull(provider.GetRequiredService<ClipboardDebounceDelay>());
    }

    [Fact]
    public async Task AddOmniaClient_AppliesConfigureToResolvedOptions()
    {
        var services = new ServiceCollection();
        services.AddOmniaClient(options => options.ForceWebSockets = true);
        await using var provider = services.BuildServiceProvider();

        Assert.True(provider.GetRequiredService<ApiOptions>().ForceWebSockets);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddOmniaClient(options => options.BaseUrl = "http://localhost:9999");
        return services.BuildServiceProvider();
    }
}

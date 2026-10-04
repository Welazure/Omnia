using Microsoft.Extensions.Logging.Abstractions;
using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Tests;

public sealed class ShellViewModelTests
{
    [Fact]
    public async Task Start_NoToken_ShowsLoginAndDoesNotStartSync()
    {
        var (shell, pages, _, clipSync) = Create();

        await shell.StartAsync(CancellationToken.None);

        Assert.Same(pages.Login, shell.CurrentPage);
        Assert.Equal(0, clipSync.StartCalls);
    }

    [Fact]
    public async Task Start_WithStoredToken_ShowsWorkspaceAndStartsSync()
    {
        var (shell, pages, tokenStore, clipSync) = Create();
        tokenStore.Token = "stored-token";

        await shell.StartAsync(CancellationToken.None);

        Assert.Same(pages.Workspace, shell.CurrentPage);
        Assert.Equal(1, clipSync.StartCalls);
        Assert.Equal("stored-token", clipSync.LastAccessToken);
    }

    [Fact]
    public async Task Start_ServerUnreachable_FallsBackToLogin()
    {
        var (shell, pages, tokenStore, clipSync) = Create();
        tokenStore.Token = "stored-token";
        clipSync.ThrowOnStart = new ApiException("server unreachable");

        await shell.StartAsync(CancellationToken.None);

        Assert.Same(pages.Login, shell.CurrentPage);
    }

    [Fact]
    public async Task ShowWorkspaceAsync_WithToken_StartsSync()
    {
        var (shell, pages, tokenStore, clipSync) = Create();
        tokenStore.Token = "stored-token";

        await shell.ShowWorkspaceAsync(CancellationToken.None);

        Assert.Same(pages.Workspace, shell.CurrentPage);
        Assert.Equal(1, clipSync.StartCalls);
    }

    [Fact]
    public async Task ShowWorkspaceAsync_NoToken_ShowsWorkspaceWithoutStartingSync()
    {
        var (shell, pages, _, clipSync) = Create();

        await shell.ShowWorkspaceAsync(CancellationToken.None);

        Assert.Same(pages.Workspace, shell.CurrentPage);
        Assert.Equal(0, clipSync.StartCalls);
    }

    [Fact]
    public void ShowAccount_ShowsAccountPage()
    {
        var (shell, pages, _, _) = Create();

        shell.ShowAccount();

        Assert.Same(pages.Account, shell.CurrentPage);
    }

    [Fact]
    public void ShowLogin_ShowsLoginPage()
    {
        var (shell, pages, _, _) = Create();

        shell.ShowLogin();

        Assert.Same(pages.Login, shell.CurrentPage);
    }

    [Fact]
    public void Navigate_RaisesCurrentPageChanged()
    {
        var (shell, _, _, _) = Create();
        var raised = 0;
        shell.CurrentPageChanged += (_, _) => raised++;

        shell.ShowLogin();

        Assert.Equal(1, raised);
    }

    private static (ShellViewModel Shell, FakePageFactory Pages, FakeTokenStore TokenStore, FakeClipSyncService ClipSync) Create()
    {
        var pages = new FakePageFactory();
        var tokenStore = new FakeTokenStore();
        var clipSync = new FakeClipSyncService();
        return (new ShellViewModel(pages, tokenStore, clipSync, NullLogger<ShellViewModel>.Instance), pages, tokenStore, clipSync);
    }
}

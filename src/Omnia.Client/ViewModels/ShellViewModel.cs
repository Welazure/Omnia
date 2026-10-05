using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Omnia.Client.Services;

namespace Omnia.Client.ViewModels;

public partial class ShellViewModel : ViewModelBase, INavigationService
{
    private readonly IPageFactory pageFactory;
    private readonly ITokenStore tokenStore;
    private readonly IClipSyncService clipSyncService;
    private readonly ClipboardSyncCoordinator autoSyncCoordinator;
    private readonly ClipboardCapability clipboardCapability;
    private readonly ILogger<ShellViewModel> logger;

    public ShellViewModel(
        IPageFactory pageFactory,
        ITokenStore tokenStore,
        IClipSyncService clipSyncService,
        ClipboardSyncCoordinator autoSyncCoordinator,
        ClipboardCapability clipboardCapability,
        ILogger<ShellViewModel> logger)
    {
        this.pageFactory = pageFactory;
        this.tokenStore = tokenStore;
        this.clipSyncService = clipSyncService;
        this.autoSyncCoordinator = autoSyncCoordinator;
        this.clipboardCapability = clipboardCapability;
        this.logger = logger;
    }

    [ObservableProperty]
    public partial ViewModelBase? CurrentPage { get; set; }

    public event EventHandler? CurrentPageChanged;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var token = await tokenStore.GetTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            ShowLogin();
            return;
        }

        try
        {
            await ShowWorkspaceAsync(cancellationToken);
        }
        catch (ApiException exception)
        {
            logger.LogWarning(exception, "Restoring the session failed; showing login.");
            ShowLogin();
        }
    }

    public async Task ShowWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        var token = await tokenStore.GetTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            await clipSyncService.StartAsync(token, cancellationToken);
            StartAutoSync();
        }

        Navigate(pageFactory.CreateWorkspace());
    }

    public void ShowLogin()
    {
        StopAutoSync();
        Navigate(pageFactory.CreateLogin());
    }

    public void ShowAccount() => Navigate(pageFactory.CreateAccount());

    private void StartAutoSync()
    {
        // A Manual head cannot read the clipboard continuously, so it never starts the monitor.
        if (clipboardCapability != ClipboardCapability.Manual)
        {
            autoSyncCoordinator.Start();
        }
    }

    private void StopAutoSync()
    {
        // Navigation is synchronous; the monitor teardown is best effort and the token/connection
        // gates already block uploads and applies once signed out.
        _ = autoSyncCoordinator.StopAsync();
    }

    private void Navigate(ViewModelBase page)
    {
        CurrentPage = page;
        CurrentPageChanged?.Invoke(this, EventArgs.Empty);
    }
}

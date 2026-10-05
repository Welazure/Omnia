using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Omnia.Client.Services;

namespace Omnia.Client.ViewModels;

public partial class ClipboardViewModel : ViewModelBase
{
    private readonly IApiClient apiClient;
    private readonly IClipSyncService clipSyncService;
    private readonly IClipboardService clipboardService;
    private readonly INavigationService navigation;
    private readonly ToastDelay toastDelay;
    private readonly IUiDispatcher dispatcher;
    private readonly IAutoSyncSettings autoSyncSettings;

    private int toastGeneration;
    private bool loadingAutoSync;

    public ClipboardViewModel(
        IApiClient apiClient,
        IClipSyncService clipSyncService,
        IClipboardService clipboardService,
        INavigationService navigation,
        ToastDelay toastDelay,
        IUiDispatcher dispatcher,
        IAutoSyncSettings autoSyncSettings)
    {
        this.apiClient = apiClient;
        this.clipSyncService = clipSyncService;
        this.clipboardService = clipboardService;
        this.navigation = navigation;
        this.toastDelay = toastDelay;
        this.dispatcher = dispatcher;
        this.autoSyncSettings = autoSyncSettings;

        clipSyncService.ClipsChanged += HandleClipsChanged;
        clipSyncService.StatusChanged += HandleStatusChanged;
        Status = clipSyncService.Status;
        RefreshAutoSyncStatus();
        RebuildClips();
        _ = LoadAutoSyncAsync();
    }

    public ObservableCollection<ClipListItem> Clips { get; } = [];

    [ObservableProperty]
    public partial string Input { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ConnectionStatus Status { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsToastVisible { get; set; }

    [ObservableProperty]
    public partial string? ToastMessage { get; set; }

    [ObservableProperty]
    public partial bool IsAutoSyncEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string AutoSyncStatusText { get; set; } = "Auto-sync on";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsConnected => Status == ConnectionStatus.Connected;

    public bool IsReconnecting => Status == ConnectionStatus.Reconnecting;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(Input))
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        var content = Input;
        Input = string.Empty;

        try
        {
            await apiClient.CreateClipAsync(content);
        }
        catch (ApiException)
        {
            Input = content;
            ErrorMessage = "Could not send the clip.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyClipAsync(ClipListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await clipboardService.SetTextAsync(item.Content);

        ToastMessage = "Copied";
        IsToastVisible = true;

        var generation = ++toastGeneration;
        await toastDelay(CancellationToken.None);

        if (generation == toastGeneration)
        {
            IsToastVisible = false;
            ToastMessage = null;
        }
    }

    [RelayCommand]
    private async Task DeleteClipAsync(ClipListItem? item)
    {
        if (item is null)
        {
            return;
        }

        Clips.Remove(item);

        try
        {
            var deleted = await apiClient.DeleteClipAsync(item.Id);
            if (!deleted)
            {
                await clipSyncService.RefreshAsync();
            }
        }
        catch (ApiException)
        {
            await clipSyncService.RefreshAsync();
        }
    }

    [RelayCommand]
    private void OpenAccount() => navigation.ShowAccount();

    partial void OnStatusChanged(ConnectionStatus value)
    {
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(IsReconnecting));
        RefreshAutoSyncStatus();
    }

    partial void OnIsAutoSyncEnabledChanged(bool value)
    {
        RefreshAutoSyncStatus();

        if (!loadingAutoSync)
        {
            _ = PersistAutoSyncAsync(value);
        }
    }

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    private async Task LoadAutoSyncAsync()
    {
        var enabled = await autoSyncSettings.GetEnabledAsync();

        loadingAutoSync = true;
        IsAutoSyncEnabled = enabled;
        loadingAutoSync = false;

        RefreshAutoSyncStatus();
    }

    private async Task PersistAutoSyncAsync(bool enabled)
    {
        try
        {
            await autoSyncSettings.SetEnabledAsync(enabled);
        }
        catch (Exception)
        {
            // A settings-file failure must not take the UI down; surface it instead.
            AutoSyncStatusText = "Couldn't save auto-sync setting";
        }
    }

    private void RefreshAutoSyncStatus()
    {
        AutoSyncStatusText = IsAutoSyncEnabled
            ? IsConnected ? "Auto-sync on" : "Auto-sync on - offline"
            : "Auto-sync paused";
    }

    private void HandleClipsChanged(object? sender, EventArgs e) => dispatcher.Post(RebuildClips);

    private void HandleStatusChanged(object? sender, ConnectionStatus status) => dispatcher.Post(() => Status = status);

    private void RebuildClips()
    {
        Clips.Clear();
        foreach (var clip in clipSyncService.Clips)
        {
            Clips.Add(new ClipListItem(clip));
        }
    }
}

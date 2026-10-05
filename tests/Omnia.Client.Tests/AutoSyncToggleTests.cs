using Microsoft.Extensions.Logging.Abstractions;
using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Tests;

// Links the user-facing toggle to the coordinator's upload gate through the shared settings seam.
public sealed class AutoSyncToggleTests
{
    [Fact]
    public async Task TogglingOff_StopsUpload_AndManualSubmitStillWorks()
    {
        var settings = new FakeAutoSyncSettings();
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        var realtime = new FakeRealtimeClient();
        realtime.RaiseStatus(ConnectionStatus.Connected);
        await using var coordinator = new ClipboardSyncCoordinator(
            monitor,
            settings,
            new FakeTokenStore { Token = "token" },
            realtime,
            api,
            delay.WaitAsync,
            NullLogger<ClipboardSyncCoordinator>.Instance);

        var viewModel = new ClipboardViewModel(
            api,
            new FakeClipSyncService(),
            new FakeClipboardService(),
            new FakeNavigationService(),
            _ => Task.CompletedTask,
            new FakeUiDispatcher(),
            settings);

        Assert.True(settings.IsEnabled);

        viewModel.IsAutoSyncEnabled = false;

        Assert.False(settings.IsEnabled);

        await Task.Run(() => monitor.RaiseClipboardTextChanged("auto"));
        delay.Release();
        await SettleAsync();

        Assert.Equal(0, api.CreateCalls);

        viewModel.Input = "manual";
        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(1, api.CreateCalls);
        Assert.Equal("manual", api.LastCreateContent);
    }

    private static async Task SettleAsync()
    {
        for (var i = 0; i < 10; i++)
        {
            await Task.Yield();
        }
    }
}

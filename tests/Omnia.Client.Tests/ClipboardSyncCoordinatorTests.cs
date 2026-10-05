using Microsoft.Extensions.Logging.Abstractions;
using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;

namespace Omnia.Client.Tests;

public sealed class ClipboardSyncCoordinatorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task ClipboardSyncCoordinator_EnabledConnectedAuthenticatedChange_UploadsExactlyOnce()
    {
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = Build(monitor, new FakeAutoSyncSettings(), new FakeTokenStore { Token = "token" }, ConnectedRealtime(), api, delay);

        await Task.Run(() => monitor.RaiseClipboardTextChanged("hello"));
        Assert.Equal(1, delay.DelayCalls);

        delay.Release();
        await api.WaitForCreateAsync().WaitAsync(Timeout);

        Assert.Equal(1, api.CreateCalls);
        Assert.Equal("hello", api.LastCreateContent);
    }

    [Fact]
    public async Task ClipboardSyncCoordinator_RepeatedIdenticalValue_DoesNotUploadAgain()
    {
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = Build(monitor, new FakeAutoSyncSettings(), new FakeTokenStore { Token = "token" }, ConnectedRealtime(), api, delay);

        await Task.Run(() => monitor.RaiseClipboardTextChanged("hello"));
        delay.Release();
        await api.WaitForCreateAsync().WaitAsync(Timeout);
        Assert.Equal(1, api.CreateCalls);

        await Task.Run(() => monitor.RaiseClipboardTextChanged("hello"));

        Assert.Equal(1, delay.DelayCalls);
        Assert.Equal(1, api.CreateCalls);
    }

    [Fact]
    public async Task ClipboardSyncCoordinator_RapidSuccessiveChanges_CoalesceToFinalValue()
    {
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = Build(monitor, new FakeAutoSyncSettings(), new FakeTokenStore { Token = "token" }, ConnectedRealtime(), api, delay);

        await Task.Run(() =>
        {
            monitor.RaiseClipboardTextChanged("A");
            monitor.RaiseClipboardTextChanged("B");
            monitor.RaiseClipboardTextChanged("C");
        });
        Assert.Equal(3, delay.DelayCalls);

        delay.Release();
        await api.WaitForCreateAsync().WaitAsync(Timeout);
        await SettleAsync();

        Assert.Equal(1, api.CreateCalls);
        Assert.Equal("C", api.LastCreateContent);
    }

    [Fact]
    public async Task ClipboardSyncCoordinator_Disabled_DoesNotUpload()
    {
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = Build(
            monitor,
            new FakeAutoSyncSettings { IsEnabled = false },
            new FakeTokenStore { Token = "token" },
            ConnectedRealtime(),
            api,
            delay);

        await Task.Run(() => monitor.RaiseClipboardTextChanged("hello"));
        delay.Release();
        await SettleAsync();

        Assert.Equal(0, api.CreateCalls);
    }

    [Fact]
    public async Task ClipboardSyncCoordinator_Disconnected_DoesNotUpload()
    {
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = Build(monitor, new FakeAutoSyncSettings(), new FakeTokenStore { Token = "token" }, new FakeRealtimeClient(), api, delay);

        await Task.Run(() => monitor.RaiseClipboardTextChanged("hello"));

        Assert.Equal(0, delay.DelayCalls);
        Assert.Equal(0, api.CreateCalls);
    }

    [Fact]
    public async Task ClipboardSyncCoordinator_Unauthenticated_DoesNotUpload()
    {
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = Build(monitor, new FakeAutoSyncSettings(), new FakeTokenStore { Token = null }, ConnectedRealtime(), api, delay);

        await Task.Run(() => monitor.RaiseClipboardTextChanged("hello"));
        delay.Release();
        await SettleAsync();

        Assert.Equal(0, api.CreateCalls);
    }

    [Fact]
    public async Task ClipboardSyncCoordinator_EmptyOrWhitespaceChange_DoesNotUpload()
    {
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = Build(monitor, new FakeAutoSyncSettings(), new FakeTokenStore { Token = "token" }, ConnectedRealtime(), api, delay);

        await Task.Run(() => monitor.RaiseClipboardTextChanged("   "));

        Assert.Equal(0, delay.DelayCalls);
        Assert.Equal(0, api.CreateCalls);
    }

    [Fact]
    public async Task ClipboardSyncCoordinator_UploadFailure_DoesNotThrow()
    {
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient { ThrowOnCreate = new ApiException("boom") };
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = Build(monitor, new FakeAutoSyncSettings(), new FakeTokenStore { Token = "token" }, ConnectedRealtime(), api, delay);

        await Task.Run(() => monitor.RaiseClipboardTextChanged("hello"));
        delay.Release();
        await api.WaitForCreateAsync().WaitAsync(Timeout);
        await SettleAsync();

        Assert.Equal(1, api.CreateCalls);
    }

    [Fact]
    public async Task ClipboardSyncCoordinator_Start_StartsTheMonitor()
    {
        var monitor = new FakeClipboardMonitor();
        var api = new FakeApiClient();
        await using var coordinator = Build(monitor, new FakeAutoSyncSettings(), new FakeTokenStore(), ConnectedRealtime(), api, new FakeClipboardDebounceDelay());

        coordinator.Start();

        Assert.Equal(1, monitor.StartCalls);
    }

    [Fact]
    public async Task ClipboardSyncCoordinator_ValueWrittenThroughGuard_IsNotUploaded()
    {
        var clipboard = new FakeClipboardService();
        var guard = new ClipboardWriteGuard(clipboard);
        var ticks = new FakeClipboardTickSource();
        var monitor = new PollingClipboardMonitor(
            clipboard,
            guard,
            ticks,
            new FakeUiDispatcher(),
            NullLogger<PollingClipboardMonitor>.Instance);
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = Build(monitor, new FakeAutoSyncSettings(), new FakeTokenStore { Token = "token" }, ConnectedRealtime(), api, delay);

        coordinator.Start();
        await AwaitTicks(ticks, 1);

        // Simulates Phase 4 applying a clip from the server through the write guard.
        await guard.WriteAsync("remote-value");
        ticks.Tick();
        await AwaitTicks(ticks, 2);
        await SettleAsync();

        Assert.Equal(0, delay.DelayCalls);
        Assert.Equal(0, api.CreateCalls);
    }

    private static FakeRealtimeClient ConnectedRealtime()
    {
        var realtime = new FakeRealtimeClient();
        realtime.RaiseStatus(ConnectionStatus.Connected);
        return realtime;
    }

    private static ClipboardSyncCoordinator Build(
        IClipboardMonitor monitor,
        FakeAutoSyncSettings settings,
        FakeTokenStore tokenStore,
        FakeRealtimeClient realtime,
        FakeApiClient api,
        FakeClipboardDebounceDelay delay) =>
        new(
            monitor,
            settings,
            tokenStore,
            realtime,
            api,
            delay.WaitAsync,
            NullLogger<ClipboardSyncCoordinator>.Instance);

    private static Task AwaitTicks(FakeClipboardTickSource ticks, int target) =>
        ticks.WaitForEntriesAsync(target).WaitAsync(Timeout);

    private static async Task SettleAsync()
    {
        for (var i = 0; i < 10; i++)
        {
            await Task.Yield();
        }
    }
}

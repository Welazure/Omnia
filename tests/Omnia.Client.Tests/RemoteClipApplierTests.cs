using Microsoft.Extensions.Logging.Abstractions;
using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests;

public sealed class RemoteClipApplierTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task RemoteClipApplier_RemoteClip_WritesContentToClipboard()
    {
        var clipboard = new FakeClipboardService();
        var realtime = new FakeRealtimeClient();
        await using var applier = Build(clipboard, realtime, new FakeAutoSyncSettings(), new FakeDeviceIdStore { DeviceId = "local" });

        realtime.RaiseClipCreated(Clip("from-phone", "remote"));
        await SettleAsync();

        Assert.Equal(1, clipboard.SetTextCalls);
        Assert.Equal("from-phone", clipboard.LastText);
    }

    [Fact]
    public async Task RemoteClipApplier_OwnClip_IsNotApplied()
    {
        var clipboard = new FakeClipboardService();
        var realtime = new FakeRealtimeClient();
        await using var applier = Build(clipboard, realtime, new FakeAutoSyncSettings(), new FakeDeviceIdStore { DeviceId = "local" });

        realtime.RaiseClipCreated(Clip("mine", "local"));
        await SettleAsync();

        Assert.Equal(0, clipboard.SetTextCalls);
    }

    [Fact]
    public async Task RemoteClipApplier_Disabled_DoesNotApply()
    {
        var clipboard = new FakeClipboardService();
        var realtime = new FakeRealtimeClient();
        await using var applier = Build(clipboard, realtime, new FakeAutoSyncSettings { IsEnabled = false }, new FakeDeviceIdStore { DeviceId = "local" });

        realtime.RaiseClipCreated(Clip("from-phone", "remote"));
        await SettleAsync();

        Assert.Equal(0, clipboard.SetTextCalls);
    }

    [Fact]
    public async Task RemoteClipApplier_EmptyContent_DoesNotApply()
    {
        var clipboard = new FakeClipboardService();
        var realtime = new FakeRealtimeClient();
        await using var applier = Build(clipboard, realtime, new FakeAutoSyncSettings(), new FakeDeviceIdStore { DeviceId = "local" });

        realtime.RaiseClipCreated(Clip("   ", "remote"));
        await SettleAsync();

        Assert.Equal(0, clipboard.SetTextCalls);
    }

    [Fact]
    public async Task RemoteClipApplier_ApplyFailure_DoesNotThrow()
    {
        var clipboard = new FakeClipboardService { ThrowOnSet = new InvalidOperationException("boom") };
        var realtime = new FakeRealtimeClient();
        await using var applier = Build(clipboard, realtime, new FakeAutoSyncSettings(), new FakeDeviceIdStore { DeviceId = "local" });

        realtime.RaiseClipCreated(Clip("from-phone", "remote"));
        await SettleAsync();

        Assert.Equal(1, clipboard.SetTextCalls);
    }

    [Fact]
    public async Task RemoteClipApplier_AppliedRemoteClip_IsNotUploaded()
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
        var realtime = ConnectedRealtime();
        var api = new FakeApiClient();
        var delay = new FakeClipboardDebounceDelay();
        await using var coordinator = new ClipboardSyncCoordinator(
            monitor,
            new FakeAutoSyncSettings(),
            new FakeTokenStore { Token = "token" },
            realtime,
            api,
            delay.WaitAsync,
            NullLogger<ClipboardSyncCoordinator>.Instance);
        await using var applier = new RemoteClipApplier(
            realtime,
            new FakeAutoSyncSettings(),
            new FakeDeviceIdStore { DeviceId = "local" },
            guard,
            NullLogger<RemoteClipApplier>.Instance);

        coordinator.Start();
        await AwaitTicks(ticks, 1);

        realtime.RaiseClipCreated(Clip("remote-value", "other"));
        await SettleAsync();

        ticks.Tick();
        await AwaitTicks(ticks, 2);
        await SettleAsync();

        Assert.Equal(1, clipboard.SetTextCalls);
        Assert.Equal("remote-value", clipboard.LastText);
        Assert.Equal(0, delay.DelayCalls);
        Assert.Equal(0, api.CreateCalls);
    }

    private static ClipDto Clip(string content, string? deviceId) =>
        new(Guid.NewGuid(), content, deviceId, DateTimeOffset.UtcNow);

    private static FakeRealtimeClient ConnectedRealtime()
    {
        var realtime = new FakeRealtimeClient();
        realtime.RaiseStatus(ConnectionStatus.Connected);
        return realtime;
    }

    private static RemoteClipApplier Build(
        FakeClipboardService clipboard,
        FakeRealtimeClient realtime,
        FakeAutoSyncSettings settings,
        FakeDeviceIdStore deviceId) =>
        new(
            realtime,
            settings,
            deviceId,
            new ClipboardWriteGuard(clipboard),
            NullLogger<RemoteClipApplier>.Instance);

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

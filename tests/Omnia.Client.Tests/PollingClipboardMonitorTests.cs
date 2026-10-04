using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;

namespace Omnia.Client.Tests;

public sealed class PollingClipboardMonitorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task PollingClipboardMonitor_DistinctChanges_RaiseOncePerChange()
    {
        var clipboard = new FakeClipboardService();
        var ticks = new FakeClipboardTickSource();
        var received = Channel.CreateUnbounded<string>();
        await using var monitor = new PollingClipboardMonitor(
            clipboard,
            new ClipboardWriteGuard(clipboard),
            ticks,
            new FakeUiDispatcher(),
            NullLogger<PollingClipboardMonitor>.Instance);
        monitor.ClipboardTextChanged += (_, text) => received.Writer.TryWrite(text);

        monitor.Start();
        await AwaitEntries(ticks, 1);

        clipboard.Text = "alpha";
        ticks.Tick();
        Assert.Equal("alpha", await ReadNext(received));

        ticks.Tick();
        await AwaitEntries(ticks, 3);
        Assert.False(received.Reader.TryRead(out _));

        clipboard.Text = "beta";
        ticks.Tick();
        Assert.Equal("beta", await ReadNext(received));

        ticks.Tick();
        await AwaitEntries(ticks, 5);
        Assert.False(received.Reader.TryRead(out _));
    }

    [Fact]
    public async Task PollingClipboardMonitor_GuardedWrite_RaisesNoChangeEvent()
    {
        var clipboard = new FakeClipboardService();
        var guard = new ClipboardWriteGuard(clipboard);
        var ticks = new FakeClipboardTickSource();
        var fired = false;
        await using var monitor = new PollingClipboardMonitor(
            clipboard,
            guard,
            ticks,
            new FakeUiDispatcher(),
            NullLogger<PollingClipboardMonitor>.Instance);
        monitor.ClipboardTextChanged += (_, _) => fired = true;

        monitor.Start();
        await AwaitEntries(ticks, 1);

        await guard.WriteAsync("own-value");
        ticks.Tick();
        await AwaitEntries(ticks, 2);
        ticks.Tick();
        await AwaitEntries(ticks, 3);

        Assert.False(fired);
    }

    [Fact]
    public async Task PollingClipboardMonitor_GuardedWriteOfCurrentValue_DoesNotSuppressLaterExternalChange()
    {
        var clipboard = new FakeClipboardService();
        var guard = new ClipboardWriteGuard(clipboard);
        var ticks = new FakeClipboardTickSource();
        var received = Channel.CreateUnbounded<string>();
        await using var monitor = new PollingClipboardMonitor(
            clipboard,
            guard,
            ticks,
            new FakeUiDispatcher(),
            NullLogger<PollingClipboardMonitor>.Instance);
        monitor.ClipboardTextChanged += (_, text) => received.Writer.TryWrite(text);

        monitor.Start();
        await AwaitEntries(ticks, 1);

        clipboard.Text = "A";
        ticks.Tick();
        Assert.Equal("A", await ReadNext(received));
        await AwaitEntries(ticks, 2);

        // Writing the value already on the clipboard must not leave a stale guard slot.
        await guard.WriteAsync("A");
        ticks.Tick();
        await AwaitEntries(ticks, 3);
        Assert.False(received.Reader.TryRead(out _));

        clipboard.Text = "B";
        ticks.Tick();
        Assert.Equal("B", await ReadNext(received));
        await AwaitEntries(ticks, 4);

        // Re-copying the earlier value externally must still raise a change.
        clipboard.Text = "A";
        ticks.Tick();
        Assert.Equal("A", await ReadNext(received));
    }

    private static Task AwaitEntries(FakeClipboardTickSource ticks, int target) =>
        ticks.WaitForEntriesAsync(target).WaitAsync(Timeout);

    private static Task<string> ReadNext(Channel<string> channel) =>
        channel.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
}

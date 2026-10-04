using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;

namespace Omnia.Client.Tests;

public sealed class ClipboardWriteGuardTests
{
    [Fact]
    public async Task ClipboardWriteGuard_WriteAsync_WritesAndConsumesMatchingReadOnce()
    {
        var clipboard = new FakeClipboardService();
        var guard = new ClipboardWriteGuard(clipboard);

        await guard.WriteAsync("x");

        Assert.Equal("x", clipboard.Text);
        Assert.False(guard.TryConsumeWrite("y"));
        Assert.True(guard.TryConsumeWrite("x"));
        Assert.False(guard.TryConsumeWrite("x"));
    }

    [Fact]
    public async Task ClipboardWriteGuard_WriteAsync_WhenWriteThrows_ClearsPendingWrite()
    {
        var guard = new ClipboardWriteGuard(new ThrowingClipboardService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => guard.WriteAsync("x"));

        Assert.False(guard.TryConsumeWrite("x"));
    }

    private sealed class ThrowingClipboardService : IClipboardService
    {
        public Task SetTextAsync(string text, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("clipboard unavailable");

        public Task<string?> TryGetTextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }
}

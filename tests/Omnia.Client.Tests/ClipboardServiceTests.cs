using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;

namespace Omnia.Client.Tests;

public sealed class ClipboardServiceTests
{
    [Fact]
    public async Task FakeClipboardService_TryGetTextAsync_ReturnsStoredText()
    {
        var clipboard = new FakeClipboardService { Text = "hello" };

        Assert.Equal("hello", await clipboard.TryGetTextAsync());
    }

    [Fact]
    public async Task AvaloniaClipboardService_TryGetTextAsync_ReturnsNullWithoutTarget()
    {
        var service = new AvaloniaClipboardService();

        Assert.Null(await service.TryGetTextAsync());
    }
}

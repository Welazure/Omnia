using System.IO;
using Omnia.Client.Services;

namespace Omnia.Client.Tests;

public sealed class FileDeviceIdStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "omnia-client-device-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetDeviceId_NoFile_GeneratesNonEmptyGuid()
    {
        var store = new FileDeviceIdStore(directory);

        var deviceId = await store.GetDeviceIdAsync(CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(deviceId));
        Assert.True(Guid.TryParse(deviceId, out _));
    }

    [Fact]
    public async Task GetDeviceId_CalledTwice_ReturnsSameId()
    {
        var store = new FileDeviceIdStore(directory);

        var first = await store.GetDeviceIdAsync(CancellationToken.None);
        var second = await store.GetDeviceIdAsync(CancellationToken.None);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GetDeviceId_SecondInstanceOverSameDirectory_ReturnsSameId()
    {
        var first = await new FileDeviceIdStore(directory).GetDeviceIdAsync(CancellationToken.None);

        var reloaded = await new FileDeviceIdStore(directory).GetDeviceIdAsync(CancellationToken.None);

        Assert.Equal(first, reloaded);
    }

    [Fact]
    public async Task GetDeviceId_ConcurrentFirstCalls_ReturnSameId()
    {
        var store = new FileDeviceIdStore(directory);

        var ids = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => store.GetDeviceIdAsync(CancellationToken.None)));

        Assert.Single(ids.Distinct());
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

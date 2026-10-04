using System.IO;
using Omnia.Client.Services;

namespace Omnia.Client.Tests;

public sealed class FileTokenStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "omnia-client-token-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetToken_NoFile_ReturnsNull()
    {
        var store = new FileTokenStore(directory);

        Assert.Null(await store.GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Save_ThenNewInstance_ReadsBackToken()
    {
        await new FileTokenStore(directory).SaveTokenAsync("token-123", CancellationToken.None);

        var reloaded = await new FileTokenStore(directory).GetTokenAsync(CancellationToken.None);

        Assert.Equal("token-123", reloaded);
    }

    [Fact]
    public async Task Save_CalledTwice_OverwritesToken()
    {
        var store = new FileTokenStore(directory);
        await store.SaveTokenAsync("first", CancellationToken.None);
        await store.SaveTokenAsync("second", CancellationToken.None);

        Assert.Equal("second", await store.GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Clear_ExistingToken_RemovesIt()
    {
        var store = new FileTokenStore(directory);
        await store.SaveTokenAsync("token-123", CancellationToken.None);

        await store.ClearTokenAsync(CancellationToken.None);

        Assert.Null(await store.GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Clear_NoToken_DoesNotThrow()
    {
        await new FileTokenStore(directory).ClearTokenAsync(CancellationToken.None);

        Assert.Null(await new FileTokenStore(directory).GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Save_BlankToken_Throws()
    {
        var store = new FileTokenStore(directory);

        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveTokenAsync("   ", CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

using System.IO;
using Omnia.Client.Services;

namespace Omnia.Client.Tests;

public sealed class FileAutoSyncSettingsTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "omnia-client-autosync-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetEnabled_NoFile_ReturnsTrue()
    {
        var settings = new FileAutoSyncSettings(directory);

        Assert.True(await settings.GetEnabledAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetEnabled_AfterSetFalse_ReturnsFalse()
    {
        var settings = new FileAutoSyncSettings(directory);
        await settings.SetEnabledAsync(false, CancellationToken.None);

        Assert.False(await settings.GetEnabledAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SetEnabled_False_PersistsAcrossInstances()
    {
        await new FileAutoSyncSettings(directory).SetEnabledAsync(false, CancellationToken.None);

        var reloaded = await new FileAutoSyncSettings(directory).GetEnabledAsync(CancellationToken.None);

        Assert.False(reloaded);
    }

    [Fact]
    public async Task SetEnabled_TrueAfterFalse_RoundTrips()
    {
        var settings = new FileAutoSyncSettings(directory);
        await settings.SetEnabledAsync(false, CancellationToken.None);
        await settings.SetEnabledAsync(true, CancellationToken.None);

        Assert.True(await new FileAutoSyncSettings(directory).GetEnabledAsync(CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

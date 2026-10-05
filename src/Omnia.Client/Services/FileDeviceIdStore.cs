using System.IO;

namespace Omnia.Client.Services;

public sealed class FileDeviceIdStore : IDeviceIdStore
{
    private const string DeviceIdFileName = "device-id.dat";

    private readonly string directory;
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? cachedDeviceId;

    public FileDeviceIdStore(string? directory = null)
    {
        this.directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Omnia");
    }

    private string DeviceIdPath => Path.Combine(directory, DeviceIdFileName);

    // The id identifies this install, not the signed-in account, so it outlives log-outs.
    public async Task<string> GetDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        if (cachedDeviceId is not null)
        {
            return cachedDeviceId;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            cachedDeviceId ??= await ReadOrCreateAsync(cancellationToken);
            return cachedDeviceId;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<string> ReadOrCreateAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(DeviceIdPath))
        {
            var stored = await File.ReadAllTextAsync(DeviceIdPath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                return stored.Trim();
            }
        }

        var deviceId = Guid.NewGuid().ToString("D");
        Directory.CreateDirectory(directory);

        var temporaryPath = DeviceIdPath + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, deviceId, cancellationToken);
        File.Move(temporaryPath, DeviceIdPath, overwrite: true);

        return deviceId;
    }
}

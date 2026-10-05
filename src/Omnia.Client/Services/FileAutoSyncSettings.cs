using System.IO;

namespace Omnia.Client.Services;

public sealed class FileAutoSyncSettings : IAutoSyncSettings
{
    private const string SettingsFileName = "auto-sync.dat";

    private readonly string directory;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool? cachedEnabled;

    public FileAutoSyncSettings(string? directory = null)
    {
        this.directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Omnia");
    }

    private string SettingsPath => Path.Combine(directory, SettingsFileName);

    // Sync defaults on; a missing (or unreadable) file means the user has never paused it.
    public async Task<bool> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        if (cachedEnabled is { } cached)
        {
            return cached;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            cachedEnabled ??= await ReadAsync(cancellationToken);
            return cachedEnabled.Value;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(directory);

            var temporaryPath = SettingsPath + ".tmp";
            await File.WriteAllTextAsync(temporaryPath, enabled ? "true" : "false", cancellationToken);
            File.Move(temporaryPath, SettingsPath, overwrite: true);

            cachedEnabled = enabled;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(SettingsPath))
        {
            return true;
        }

        var stored = await File.ReadAllTextAsync(SettingsPath, cancellationToken);
        return bool.TryParse(stored.Trim(), out var enabled) ? enabled : true;
    }
}

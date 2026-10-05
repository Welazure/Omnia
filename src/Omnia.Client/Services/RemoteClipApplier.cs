using Microsoft.Extensions.Logging;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Services;

public sealed class RemoteClipApplier : IAsyncDisposable
{
    private readonly IRealtimeClient realtimeClient;
    private readonly IAutoSyncSettings settings;
    private readonly IDeviceIdStore deviceIdStore;
    private readonly ClipboardWriteGuard writeGuard;
    private readonly ILogger<RemoteClipApplier> logger;

    public RemoteClipApplier(
        IRealtimeClient realtimeClient,
        IAutoSyncSettings settings,
        IDeviceIdStore deviceIdStore,
        ClipboardWriteGuard writeGuard,
        ILogger<RemoteClipApplier> logger)
    {
        this.realtimeClient = realtimeClient;
        this.settings = settings;
        this.deviceIdStore = deviceIdStore;
        this.writeGuard = writeGuard;
        this.logger = logger;

        realtimeClient.ClipCreated += HandleClipCreated;
    }

    public ValueTask DisposeAsync()
    {
        realtimeClient.ClipCreated -= HandleClipCreated;
        return ValueTask.CompletedTask;
    }

    private void HandleClipCreated(object? sender, ClipDto clip) => _ = ApplyAsync(clip);

    private async Task ApplyAsync(ClipDto clip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(clip.Content))
            {
                return;
            }

            if (!await settings.GetEnabledAsync())
            {
                return;
            }

            var localDeviceId = await deviceIdStore.GetDeviceIdAsync();
            if (string.Equals(clip.DeviceId, localDeviceId, StringComparison.Ordinal))
            {
                return;
            }

            // The guard arms before the OS write, so the monitor consumes this value and never
            // raises a change event for it — an applied remote clip cannot loop back as an upload.
            await writeGuard.WriteAsync(clip.Content);
        }
        catch (Exception exception)
        {
            // Applying must never take the app down; the manual list still works.
            logger.LogWarning(exception, "Applying the incoming clip failed.");
        }
    }
}

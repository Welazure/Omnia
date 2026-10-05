using Microsoft.Extensions.Logging;

namespace Omnia.Client.Services;

public sealed class ClipboardSyncCoordinator : IAsyncDisposable
{
    public static readonly TimeSpan DefaultDebounceInterval = TimeSpan.FromMilliseconds(500);

    private readonly IClipboardMonitor monitor;
    private readonly IAutoSyncSettings settings;
    private readonly ITokenStore tokenStore;
    private readonly IRealtimeClient realtimeClient;
    private readonly IApiClient apiClient;
    private readonly ClipboardDebounceDelay debounceDelay;
    private readonly ILogger<ClipboardSyncCoordinator> logger;

    // The monitor posts changes on the UI thread while the debounce resume runs on the thread pool,
    // so the change generation and last-sent record are guarded together.
    private readonly object gate = new();
    private string? lastSent;
    private long generation;

    public ClipboardSyncCoordinator(
        IClipboardMonitor monitor,
        IAutoSyncSettings settings,
        ITokenStore tokenStore,
        IRealtimeClient realtimeClient,
        IApiClient apiClient,
        ClipboardDebounceDelay debounceDelay,
        ILogger<ClipboardSyncCoordinator> logger)
    {
        this.monitor = monitor;
        this.settings = settings;
        this.tokenStore = tokenStore;
        this.realtimeClient = realtimeClient;
        this.apiClient = apiClient;
        this.debounceDelay = debounceDelay;
        this.logger = logger;

        monitor.ClipboardTextChanged += HandleClipboardTextChanged;
    }

    public void Start() => monitor.Start();

    public ValueTask DisposeAsync()
    {
        monitor.ClipboardTextChanged -= HandleClipboardTextChanged;
        return ValueTask.CompletedTask;
    }

    private void HandleClipboardTextChanged(object? sender, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || realtimeClient.Status != ConnectionStatus.Connected)
        {
            return;
        }

        long expectedGeneration;
        lock (gate)
        {
            if (string.Equals(text, lastSent, StringComparison.Ordinal))
            {
                return;
            }

            expectedGeneration = ++generation;
        }

        _ = UploadAfterDebounceAsync(expectedGeneration, text);
    }

    private async Task UploadAfterDebounceAsync(long expectedGeneration, string value)
    {
        await debounceDelay(DefaultDebounceInterval, CancellationToken.None);

        lock (gate)
        {
            // A newer change arrived during the window; its own debounce owns the upload.
            if (generation != expectedGeneration)
            {
                return;
            }
        }

        await UploadAsync(value);
    }

    private async Task UploadAsync(string value)
    {
        try
        {
            if (!await settings.GetEnabledAsync())
            {
                return;
            }

            var token = await tokenStore.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            lock (gate)
            {
                if (string.Equals(value, lastSent, StringComparison.Ordinal))
                {
                    return;
                }

                lastSent = value;
            }

            await apiClient.CreateClipAsync(value);
        }
        catch (Exception exception)
        {
            // A local copy must never take the app down; the manual list still works.
            logger.LogWarning(exception, "Uploading the local clipboard change failed.");
        }
    }
}

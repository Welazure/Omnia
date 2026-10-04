using Microsoft.Extensions.Logging;

namespace Omnia.Client.Services;

public sealed class PollingClipboardMonitor : IClipboardMonitor
{
    private readonly IClipboardService clipboard;
    private readonly ClipboardWriteGuard writeGuard;
    private readonly IClipboardTickSource tickSource;
    private readonly IUiDispatcher dispatcher;
    private readonly ILogger<PollingClipboardMonitor> logger;

    private CancellationTokenSource? cancellation;
    private Task? loop;
    private string? lastText;

    public PollingClipboardMonitor(
        IClipboardService clipboard,
        ClipboardWriteGuard writeGuard,
        IClipboardTickSource tickSource,
        IUiDispatcher dispatcher,
        ILogger<PollingClipboardMonitor> logger)
    {
        this.clipboard = clipboard;
        this.writeGuard = writeGuard;
        this.tickSource = tickSource;
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    public event EventHandler<string>? ClipboardTextChanged;

    public void Start()
    {
        if (cancellation is not null)
        {
            return;
        }

        cancellation = new CancellationTokenSource();
        loop = RunAsync(cancellation.Token);
    }

    public async ValueTask DisposeAsync()
    {
        CancellationTokenSource? cts = cancellation;
        Task? running = loop;

        if (cts is null)
        {
            return;
        }

        cancellation = null;
        loop = null;

        await cts.CancelAsync();

        if (running is not null)
        {
            await running;
        }

        cts.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            bool ticked;
            try
            {
                ticked = await tickSource.WaitForTickAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!ticked)
            {
                break;
            }

            await PollOnceAsync(cancellationToken);
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        string? text;
        try
        {
            text = await clipboard.TryGetTextAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Reading the clipboard failed; skipping this poll.");
            return;
        }

        if (text is null)
        {
            return;
        }

        bool suppressed = writeGuard.TryConsumeWrite(text);
        bool changed = !string.Equals(text, lastText, StringComparison.Ordinal);
        lastText = text;

        if (suppressed || !changed)
        {
            return;
        }

        dispatcher.Post(() => ClipboardTextChanged?.Invoke(this, text));
    }
}

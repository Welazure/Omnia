namespace Omnia.Client.Services;

public sealed class ClipboardWriteGuard
{
    private readonly IClipboardService clipboard;
    private readonly object gate = new();
    private string? pendingWrite;

    public ClipboardWriteGuard(IClipboardService clipboard) => this.clipboard = clipboard;

    public async Task WriteAsync(string text, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            pendingWrite = text;
        }

        try
        {
            await clipboard.SetTextAsync(text, cancellationToken);
        }
        catch
        {
            lock (gate)
            {
                if (string.Equals(pendingWrite, text, StringComparison.Ordinal))
                {
                    pendingWrite = null;
                }
            }

            throw;
        }
    }

    public bool TryConsumeWrite(string text)
    {
        lock (gate)
        {
            if (!string.Equals(pendingWrite, text, StringComparison.Ordinal))
            {
                return false;
            }

            pendingWrite = null;
            return true;
        }
    }
}

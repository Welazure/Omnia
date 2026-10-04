using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace Omnia.Client.Services;

public sealed class AvaloniaClipboardService : IClipboardService
{
    private Control? target;

    public void Attach(Control control) => target = control;

    public async Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var clipboard = ResolveClipboard();
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    public async Task<string?> TryGetTextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var clipboard = ResolveClipboard();
        return clipboard is null ? null : await clipboard.TryGetTextAsync();
    }

    private IClipboard? ResolveClipboard() => target is null ? null : TopLevel.GetTopLevel(target)?.Clipboard;
}

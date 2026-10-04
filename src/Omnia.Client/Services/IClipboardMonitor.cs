namespace Omnia.Client.Services;

public interface IClipboardMonitor : IAsyncDisposable
{
    event EventHandler<string>? ClipboardTextChanged;

    void Start();
}

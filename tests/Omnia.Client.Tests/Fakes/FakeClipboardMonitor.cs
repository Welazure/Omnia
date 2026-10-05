using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeClipboardMonitor : IClipboardMonitor
{
    public event EventHandler<string>? ClipboardTextChanged;

    public int StartCalls { get; private set; }

    public void Start() => StartCalls++;

    public void RaiseClipboardTextChanged(string text) => ClipboardTextChanged?.Invoke(this, text);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

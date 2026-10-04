using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeClipboardService : IClipboardService
{
    public string? LastText { get; private set; }

    public int SetTextCalls { get; private set; }

    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        SetTextCalls++;
        LastText = text;
        return Task.CompletedTask;
    }
}

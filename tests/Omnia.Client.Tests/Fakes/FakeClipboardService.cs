using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeClipboardService : IClipboardService
{
    public string? Text { get; set; }

    public string? LastText { get; private set; }

    public int SetTextCalls { get; private set; }

    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        SetTextCalls++;
        LastText = text;
        Text = text;
        return Task.CompletedTask;
    }

    public Task<string?> TryGetTextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Text);
}

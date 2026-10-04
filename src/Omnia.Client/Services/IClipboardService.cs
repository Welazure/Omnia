namespace Omnia.Client.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text, CancellationToken cancellationToken = default);

    Task<string?> TryGetTextAsync(CancellationToken cancellationToken = default);
}

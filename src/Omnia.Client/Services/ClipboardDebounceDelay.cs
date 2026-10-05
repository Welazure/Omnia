namespace Omnia.Client.Services;

public delegate Task ClipboardDebounceDelay(TimeSpan duration, CancellationToken cancellationToken);

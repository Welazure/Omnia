using Omnia.Shared.Contracts;

namespace Omnia.Client.ViewModels;

public sealed class ClipListItem(ClipDto clip)
{
    public Guid Id { get; } = clip.Id;

    public string Content { get; } = clip.Content;

    public DateTimeOffset CreatedAt { get; } = clip.CreatedAt;
}

namespace Omnia.Api.Data;

public sealed class Clip
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string Content { get; set; }

    public string? DeviceId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

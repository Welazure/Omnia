namespace Omnia.Shared.Contracts;

public sealed record ClipDto(Guid Id, string Content, string? DeviceId, DateTimeOffset CreatedAt);

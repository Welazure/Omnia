using Omnia.Shared.Contracts;

namespace Omnia.Api.Services;

public interface IClipService
{
    Task<IReadOnlyList<ClipDto>> GetClipsAsync(Guid userId, CancellationToken cancellationToken);

    Task<ClipDto> CreateAsync(Guid userId, CreateClipRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid clipId, CancellationToken cancellationToken);
}

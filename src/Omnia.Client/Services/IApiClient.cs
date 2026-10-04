using Omnia.Shared.Contracts;

namespace Omnia.Client.Services;

public interface IApiClient
{
    Task<AuthResponse?> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<UserDto?> GetMeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClipDto>> GetClipsAsync(CancellationToken cancellationToken = default);

    Task<ClipDto?> CreateClipAsync(string content, string? deviceId, CancellationToken cancellationToken = default);

    Task<bool> DeleteClipAsync(Guid id, CancellationToken cancellationToken = default);
}

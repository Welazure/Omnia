using Omnia.Client.Services;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeApiClient : IApiClient
{
    public AuthResponse? RegisterResult { get; set; }

    public AuthResponse? LoginResult { get; set; }

    public IReadOnlyList<ClipDto> ClipsResult { get; set; } = [];

    public Exception? ThrowOnLogin { get; set; }

    public int RegisterCalls { get; private set; }

    public int LoginCalls { get; private set; }

    public int GetClipsCalls { get; private set; }

    public Task<AuthResponse?> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        RegisterCalls++;
        return Task.FromResult(RegisterResult);
    }

    public Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        LoginCalls++;
        return ThrowOnLogin is null ? Task.FromResult(LoginResult) : Task.FromException<AuthResponse?>(ThrowOnLogin);
    }

    public Task<IReadOnlyList<ClipDto>> GetClipsAsync(CancellationToken cancellationToken = default)
    {
        GetClipsCalls++;
        return Task.FromResult(ClipsResult);
    }

    public Task<ClipDto?> CreateClipAsync(string content, string? deviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ClipDto?>(null);

    public Task<bool> DeleteClipAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

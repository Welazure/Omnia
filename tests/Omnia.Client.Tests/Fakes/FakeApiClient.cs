using Omnia.Client.Services;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeApiClient : IApiClient
{
    public AuthResponse? RegisterResult { get; set; }

    public AuthResponse? LoginResult { get; set; }

    public UserDto? MeResult { get; set; }

    public IReadOnlyList<ClipDto> ClipsResult { get; set; } = [];

    public ClipDto? CreateResult { get; set; }

    public bool DeleteResult { get; set; } = true;

    public Exception? ThrowOnLogin { get; set; }

    public Exception? ThrowOnCreate { get; set; }

    public Exception? ThrowOnDelete { get; set; }

    public Exception? ThrowOnGetMe { get; set; }

    public int RegisterCalls { get; private set; }

    public int LoginCalls { get; private set; }

    public int GetMeCalls { get; private set; }

    public int GetClipsCalls { get; private set; }

    public int CreateCalls { get; private set; }

    public int DeleteCalls { get; private set; }

    public string? LastCreateContent { get; private set; }

    public Guid? LastDeleteId { get; private set; }

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

    public Task<UserDto?> GetMeAsync(CancellationToken cancellationToken = default)
    {
        GetMeCalls++;
        return ThrowOnGetMe is null ? Task.FromResult(MeResult) : Task.FromException<UserDto?>(ThrowOnGetMe);
    }

    public Task<IReadOnlyList<ClipDto>> GetClipsAsync(CancellationToken cancellationToken = default)
    {
        GetClipsCalls++;
        return Task.FromResult(ClipsResult);
    }

    public Task<ClipDto?> CreateClipAsync(string content, CancellationToken cancellationToken = default)
    {
        CreateCalls++;
        LastCreateContent = content;
        return ThrowOnCreate is null ? Task.FromResult(CreateResult) : Task.FromException<ClipDto?>(ThrowOnCreate);
    }

    public Task<bool> DeleteClipAsync(Guid id, CancellationToken cancellationToken = default)
    {
        DeleteCalls++;
        LastDeleteId = id;
        return ThrowOnDelete is null ? Task.FromResult(DeleteResult) : Task.FromException<bool>(ThrowOnDelete);
    }
}

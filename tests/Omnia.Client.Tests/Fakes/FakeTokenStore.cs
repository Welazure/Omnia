using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeTokenStore : ITokenStore
{
    public string? Token { get; set; }

    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult(Token);

    public Task SaveTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        Token = token;
        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(CancellationToken cancellationToken = default)
    {
        Token = null;
        return Task.CompletedTask;
    }
}

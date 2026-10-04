using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeHubConnectionFactory : IHubConnectionFactory
{
    public FakeHubConnectionAdapter Adapter { get; } = new();

    public int CreateCalls { get; private set; }

    public string? LastAccessToken { get; private set; }

    public IHubConnectionAdapter Create(string accessToken)
    {
        CreateCalls++;
        LastAccessToken = accessToken;
        return Adapter;
    }
}

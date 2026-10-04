namespace Omnia.Client.Services;

public interface IHubConnectionFactory
{
    IHubConnectionAdapter Create(string accessToken);
}

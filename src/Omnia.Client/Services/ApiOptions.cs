namespace Omnia.Client.Services;

public sealed class ApiOptions
{
    public const string DefaultBaseUrl = "http://localhost:8080";

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public bool ForceWebSockets { get; set; }

    public Uri BaseUri => new(BaseUrl.TrimEnd('/') + "/");
}

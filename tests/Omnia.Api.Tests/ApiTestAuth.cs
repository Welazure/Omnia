using System.Net.Http.Headers;
using System.Net.Http.Json;
using Omnia.Shared.Contracts;
using Omnia.Shared.Http;

namespace Omnia.Api.Tests;

internal static class ApiTestAuth
{
    public const string Password = "correct-horse-battery-staple";

    public static async Task<AuthResponse> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(ApiRoutes.Register, new RegisterRequest(email, Password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static async Task<AuthResponse> RegisterAndAuthorizeAsync(HttpClient client, string email)
    {
        var auth = await RegisterAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return auth;
    }
}

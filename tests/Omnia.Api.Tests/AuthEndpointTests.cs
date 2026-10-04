using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Omnia.Api.Data;
using Omnia.Shared.Contracts;
using Omnia.Shared.Http;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class AuthEndpointTests(PostgresContainerFixture fixture)
{
    private const string Password = "correct-horse-battery-staple";

    [Fact]
    public async Task Register_NewEmail_Returns201WithTokenAndUser()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            ApiRoutes.Register, new RegisterRequest("New.User@Example.com", Password));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Equal("new.user@example.com", auth.User.Email);
        Assert.NotEqual(Guid.Empty, auth.User.Id);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync(ApiRoutes.Register, new RegisterRequest("dup@example.com", Password));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync(ApiRoutes.Register, new RegisterRequest("dup@example.com", Password));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Register_DuplicateEmailDifferentCase_Returns409()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync(ApiRoutes.Register, new RegisterRequest("case@example.com", Password));

        var duplicate = await client.PostAsJsonAsync(
            ApiRoutes.Register, new RegisterRequest("CASE@Example.com", Password));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Register_EmptyPassword_Returns400()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(ApiRoutes.Register, new RegisterRequest("empty@example.com", "  "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();
        await RegisterAsync(client, "login@example.com");

        var response = await client.PostAsJsonAsync(ApiRoutes.Login, new LoginRequest("login@example.com", Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Equal("login@example.com", auth.User.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();
        await RegisterAsync(client, "wrong@example.com");

        var response = await client.PostAsJsonAsync(ApiRoutes.Login, new LoginRequest("wrong@example.com", "not-the-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(ApiRoutes.Login, new LoginRequest("missing@example.com", Password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.Me);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithMalformedToken_Returns401()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync(ApiRoutes.Me);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200WithUser()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();
        var registered = await RegisterAsync(client, "me@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.Token);

        var response = await client.GetAsync(ApiRoutes.Me);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal(registered.User.Id, user.Id);
        Assert.Equal("me@example.com", user.Email);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlaintext()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync(ApiRoutes.Register, new RegisterRequest("stored@example.com", Password));

        await using var context = TestDatabase.CreateContext(connectionString);
        var user = await context.Users.SingleAsync(candidate => candidate.Email == "stored@example.com");

        Assert.NotEqual(Password, user.PasswordHash);
        Assert.DoesNotContain(Password, user.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, Password));
    }

    [Fact]
    public async Task RegisterAndLogin_DoNotLogPlaintextPassword()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var loggerProvider = new CapturingLoggerProvider();
        using var factory = ApiFactory.Create(connectionString, loggerProvider);
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync(ApiRoutes.Register, new RegisterRequest("logs@example.com", Password));
        await client.PostAsJsonAsync(ApiRoutes.Login, new LoginRequest("logs@example.com", Password));

        Assert.NotEmpty(loggerProvider.Messages);
        Assert.DoesNotContain(loggerProvider.Messages, message => message.Contains(Password, StringComparison.Ordinal));
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(ApiRoutes.Register, new RegisterRequest(email, Password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}

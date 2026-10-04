using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Omnia.Api.Auth;
using Omnia.Api.Data;
using Omnia.Api.Services;
using Omnia.Shared.Contracts;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class AuthServiceTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task Register_DuplicateEmail_ReturnsNull()
    {
        var (service, _) = await CreateServiceAsync();

        var first = await service.RegisterAsync(new RegisterRequest("dup@example.com", "first-password"), CancellationToken.None);
        var second = await service.RegisterAsync(new RegisterRequest("dup@example.com", "second-password"), CancellationToken.None);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task Register_StoresHashAndReturnsToken()
    {
        var (service, context) = await CreateServiceAsync();

        var result = await service.RegisterAsync(new RegisterRequest("store@example.com", "secret-password"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        var stored = await context.Users.SingleAsync(candidate => candidate.Email == "store@example.com");
        Assert.NotEqual("secret-password", stored.PasswordHash);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        var (service, _) = await CreateServiceAsync();
        await service.RegisterAsync(new RegisterRequest("login@example.com", "right-password"), CancellationToken.None);

        var result = await service.LoginAsync(new LoginRequest("login@example.com", "wrong-password"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var (service, _) = await CreateServiceAsync();
        await service.RegisterAsync(new RegisterRequest("valid@example.com", "right-password"), CancellationToken.None);

        var result = await service.LoginAsync(new LoginRequest("valid@example.com", "right-password"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Register_NonUniqueDbFailure_PropagatesException()
    {
        var (service, context) = await CreateServiceAsync();
        await context.Database.ExecuteSqlRawAsync(
            """ALTER TABLE "Users" ADD CONSTRAINT "CK_Users_BlockedTest" CHECK ("Email" <> 'blocked@example.com');""");

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.RegisterAsync(new RegisterRequest("blocked@example.com", "secret-password"), CancellationToken.None));
    }

    [Fact]
    public async Task GetUser_UnknownId_ReturnsNull()
    {
        var (service, _) = await CreateServiceAsync();

        var result = await service.GetUserAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    private async Task<(AuthService Service, OmniaDbContext Context)> CreateServiceAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var context = TestDatabase.CreateContext(connectionString);
        await context.Database.MigrateAsync();

        var service = new AuthService(
            context,
            new PasswordHasher<User>(),
            new JwtTokenService(new JwtOptions { Secret = ApiFactory.JwtSecret }),
            NullLogger<AuthService>.Instance);

        return (service, context);
    }
}

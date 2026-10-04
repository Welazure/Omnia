using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class StartupMigrationTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task Api_Startup_AppliesMigrations()
    {
        var connectionString = await fixture.CreateDatabaseAsync();

        using var factory = ApiFactory.Create(connectionString);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var context = TestDatabase.CreateContext(connectionString);
        var applied = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, name => name.EndsWith("InitialCreate"));

        Assert.Equal(0, await context.Clips.CountAsync());
    }
}

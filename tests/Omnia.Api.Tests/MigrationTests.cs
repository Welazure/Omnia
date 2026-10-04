using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class MigrationTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task InitialCreate_Migrate_CreatesSchemaAndHistory()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var context = TestDatabase.CreateContext(connectionString);

        await context.Database.MigrateAsync();

        var applied = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, name => name.EndsWith("InitialCreate"));

        var tables = await TestDatabase.QueryTablesAsync(connectionString);
        Assert.Contains("Users", tables);
        Assert.Contains("Clips", tables);
    }

    [Fact]
    public async Task InitialCreate_RollbackToZero_DropsSchema()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var context = TestDatabase.CreateContext(connectionString);
        await context.Database.MigrateAsync();

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("0");

        var tables = await TestDatabase.QueryTablesAsync(connectionString);
        Assert.DoesNotContain("Users", tables);
        Assert.DoesNotContain("Clips", tables);
    }

    [Fact]
    public async Task MigratedSchema_TableColumns_MatchArchitecture()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var context = TestDatabase.CreateContext(connectionString);
        await context.Database.MigrateAsync();

        var columns = await TestDatabase.QueryColumnTypesAsync(connectionString);

        Assert.Equal("uuid", columns[("Users", "Id")]);
        Assert.Equal("text", columns[("Users", "Email")]);
        Assert.Equal("text", columns[("Users", "PasswordHash")]);
        Assert.Equal("timestamp with time zone", columns[("Users", "CreatedAt")]);

        Assert.Equal("uuid", columns[("Clips", "Id")]);
        Assert.Equal("uuid", columns[("Clips", "UserId")]);
        Assert.Equal("text", columns[("Clips", "Content")]);
        Assert.Equal("text", columns[("Clips", "DeviceId")]);
        Assert.Equal("timestamp with time zone", columns[("Clips", "CreatedAt")]);
    }
}

using Microsoft.EntityFrameworkCore;
using Omnia.Api.Data;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class UniqueEmailIndexTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task Users_DuplicateEmail_ThrowsUniqueViolation()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var context = TestDatabase.CreateContext(connectionString);
        await context.Database.MigrateAsync();

        context.Users.Add(CreateUser("duplicate@example.com", "hash-one"));
        await context.SaveChangesAsync();

        context.Users.Add(CreateUser("duplicate@example.com", "hash-two"));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Users_DuplicateEmailDifferentCase_ThrowsUniqueViolation()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var context = TestDatabase.CreateContext(connectionString);
        await context.Database.MigrateAsync();

        context.Users.Add(CreateUser("case@example.com", "hash-one"));
        await context.SaveChangesAsync();

        context.Users.Add(CreateUser("CASE@Example.com", "hash-two"));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static User CreateUser(string email, string passwordHash) => new()
    {
        Email = email,
        PasswordHash = passwordHash,
        CreatedAt = DateTimeOffset.UtcNow
    };
}

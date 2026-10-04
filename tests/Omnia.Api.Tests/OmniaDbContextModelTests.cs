using Microsoft.EntityFrameworkCore;
using Omnia.Api.Data;

namespace Omnia.Api.Tests;

public class OmniaDbContextModelTests
{
    [Fact]
    public void OmniaDbContext_Model_UserColumnsMatchArchitecture()
    {
        using var context = CreateContext();
        var user = context.Model.FindEntityType(typeof(User))!;

        Assert.Equal("Users", user.GetTableName());
        Assert.Equal(
            new[] { "CreatedAt", "Email", "Id", "PasswordHash" },
            user.GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray());

        Assert.Equal("Id", user.FindPrimaryKey()!.Properties.Single().Name);

        var emailIndex = user.GetIndexes().Single();
        Assert.True(emailIndex.IsUnique);
        Assert.Equal("Email", emailIndex.Properties.Single().Name);
    }

    [Fact]
    public void OmniaDbContext_Model_ClipColumnsMatchArchitecture()
    {
        using var context = CreateContext();
        var clip = context.Model.FindEntityType(typeof(Clip))!;

        Assert.Equal("Clips", clip.GetTableName());
        Assert.Equal(
            new[] { "Content", "CreatedAt", "DeviceId", "Id", "UserId" },
            clip.GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray());

        Assert.Equal("Id", clip.FindPrimaryKey()!.Properties.Single().Name);
        Assert.True(clip.FindProperty("DeviceId")!.IsNullable);

        var foreignKey = clip.GetForeignKeys().Single();
        Assert.Equal("UserId", foreignKey.Properties.Single().Name);
        Assert.Equal("Users", foreignKey.PrincipalEntityType.GetTableName());

        var userIdIndex = clip.GetIndexes().Single();
        Assert.Equal("UserId", userIdIndex.Properties.Single().Name);
        Assert.False(userIdIndex.IsUnique);
    }

    private static OmniaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<OmniaDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=omnia_model;Username=omnia;Password=omnia")
            .Options;

        return new OmniaDbContext(options);
    }
}

using Omnia.Api.Data;

namespace Omnia.Api.Tests;

internal static class ClipSeeder
{
    public static async Task<Guid> AddUserAsync(string connectionString, string email)
    {
        await using var context = TestDatabase.CreateContext(connectionString);
        var id = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = id,
            Email = email,
            PasswordHash = "seeded-hash",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
        return id;
    }

    public static async Task<Guid> AddClipAsync(
        string connectionString,
        Guid userId,
        string content,
        DateTimeOffset createdAt,
        string? deviceId = null)
    {
        await using var context = TestDatabase.CreateContext(connectionString);
        var id = Guid.NewGuid();
        context.Clips.Add(new Clip
        {
            Id = id,
            UserId = userId,
            Content = content,
            DeviceId = deviceId,
            CreatedAt = createdAt
        });
        await context.SaveChangesAsync();
        return id;
    }
}

using Microsoft.EntityFrameworkCore;
using Omnia.Api.Data;
using Omnia.Api.Hubs;
using Omnia.Api.Services;
using Omnia.Shared.Contracts;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class ClipServiceTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task GetClipsAsync_ReturnsOnlyCallersClipsNewestFirst()
    {
        var (service, context) = await CreateServiceAsync();
        var caller = await SeedUserAsync(context, "caller@example.com");
        var other = await SeedUserAsync(context, "other@example.com");
        await SeedClipAsync(context, caller, "caller-old", DateTimeOffset.UtcNow.AddMinutes(-5));
        await SeedClipAsync(context, caller, "caller-new", DateTimeOffset.UtcNow);
        await SeedClipAsync(context, other, "other-only", DateTimeOffset.UtcNow);

        var clips = await service.GetClipsAsync(caller, CancellationToken.None);

        Assert.Equal(new[] { "caller-new", "caller-old" }, clips.Select(clip => clip.Content));
    }

    [Fact]
    public async Task CreateAsync_ReturnsPersistedDto()
    {
        var (service, context) = await CreateServiceAsync();
        var caller = await SeedUserAsync(context, "create@example.com");

        var dto = await service.CreateAsync(caller, new CreateClipRequest("hello", "device-1"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("hello", dto.Content);
        Assert.Equal("device-1", dto.DeviceId);

        var stored = await context.Clips.SingleAsync(clip => clip.Id == dto.Id);
        Assert.Equal(caller, stored.UserId);
        Assert.Equal("hello", stored.Content);
    }

    [Fact]
    public async Task CreateAsync_NotifiesCallerGroupOnly()
    {
        var hub = new RecordingHubContext();
        var (service, context) = await CreateServiceAsync(hub);
        var caller = await SeedUserAsync(context, "notify@example.com");

        var dto = await service.CreateAsync(caller, new CreateClipRequest("secret", null), CancellationToken.None);

        var (groupName, clip) = Assert.Single(hub.Created);
        Assert.Equal(ClipHub.GroupName(caller), groupName);
        Assert.Equal(dto.Id, clip.Id);
        Assert.Empty(hub.Deleted);
    }

    [Fact]
    public async Task DeleteAsync_OwnClip_RemovesAndNotifies()
    {
        var hub = new RecordingHubContext();
        var (service, context) = await CreateServiceAsync(hub);
        var caller = await SeedUserAsync(context, "delete@example.com");
        var clipId = await SeedClipAsync(context, caller, "gone", DateTimeOffset.UtcNow);

        var deleted = await service.DeleteAsync(caller, clipId, CancellationToken.None);

        Assert.True(deleted);
        Assert.False(await context.Clips.AnyAsync(clip => clip.Id == clipId));
        var (groupName, notifiedId) = Assert.Single(hub.Deleted);
        Assert.Equal(ClipHub.GroupName(caller), groupName);
        Assert.Equal(clipId, notifiedId);
    }

    [Fact]
    public async Task DeleteAsync_OtherUsersClip_ReturnsFalse()
    {
        var hub = new RecordingHubContext();
        var (service, context) = await CreateServiceAsync(hub);
        var owner = await SeedUserAsync(context, "owner@example.com");
        var intruder = await SeedUserAsync(context, "intruder@example.com");
        var clipId = await SeedClipAsync(context, owner, "mine", DateTimeOffset.UtcNow);

        var deleted = await service.DeleteAsync(intruder, clipId, CancellationToken.None);

        Assert.False(deleted);
        Assert.True(await context.Clips.AnyAsync(clip => clip.Id == clipId));
        Assert.Empty(hub.Deleted);
    }

    private async Task<(ClipService Service, OmniaDbContext Context)> CreateServiceAsync(
        RecordingHubContext? hub = null)
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var context = TestDatabase.CreateContext(connectionString);
        await context.Database.MigrateAsync();

        return (new ClipService(context, hub ?? new RecordingHubContext()), context);
    }

    private static async Task<Guid> SeedUserAsync(OmniaDbContext context, string email)
    {
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

    private static async Task<Guid> SeedClipAsync(
        OmniaDbContext context,
        Guid userId,
        string content,
        DateTimeOffset createdAt)
    {
        var id = Guid.NewGuid();
        context.Clips.Add(new Clip
        {
            Id = id,
            UserId = userId,
            Content = content,
            CreatedAt = createdAt
        });
        await context.SaveChangesAsync();
        return id;
    }
}

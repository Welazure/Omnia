using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Omnia.Api.Data;
using Omnia.Api.Hubs;
using Omnia.Shared.Contracts;
using Omnia.Shared.Realtime;

namespace Omnia.Api.Services;

public sealed class ClipService(
    OmniaDbContext database,
    IHubContext<ClipHub, IClipClient> hubContext) : IClipService
{
    public async Task<IReadOnlyList<ClipDto>> GetClipsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await database.Clips
            .Where(clip => clip.UserId == userId)
            .OrderByDescending(clip => clip.CreatedAt)
            .ThenByDescending(clip => clip.Id)
            .Select(clip => new ClipDto(clip.Id, clip.Content, clip.DeviceId, clip.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClipDto> CreateAsync(Guid userId, CreateClipRequest request, CancellationToken cancellationToken)
    {
        // Deduplicate only against the user's newest clip: the list is a timeline, so identical
        // content may legitimately reappear later and must not be treated as a global duplicate.
        var newest = await database.Clips
            .AsNoTracking()
            .Where(clip => clip.UserId == userId)
            .OrderByDescending(clip => clip.CreatedAt)
            .ThenByDescending(clip => clip.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (newest is not null && newest.Content == request.Content)
        {
            return new ClipDto(newest.Id, newest.Content, newest.DeviceId, newest.CreatedAt);
        }

        var clip = new Clip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = request.Content,
            DeviceId = request.DeviceId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        database.Clips.Add(clip);
        await database.SaveChangesAsync(cancellationToken);

        var dto = new ClipDto(clip.Id, clip.Content, clip.DeviceId, clip.CreatedAt);
        await hubContext.Clients.Group(ClipHub.GroupName(userId)).OnClipCreated(dto);
        return dto;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid clipId, CancellationToken cancellationToken)
    {
        var clip = await database.Clips.SingleOrDefaultAsync(
            candidate => candidate.Id == clipId && candidate.UserId == userId, cancellationToken);
        if (clip is null)
        {
            return false;
        }

        database.Clips.Remove(clip);
        await database.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group(ClipHub.GroupName(userId)).OnClipDeleted(clipId);
        return true;
    }
}

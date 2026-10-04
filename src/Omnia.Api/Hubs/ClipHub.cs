using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Omnia.Api.Auth;
using Omnia.Shared.Realtime;

namespace Omnia.Api.Hubs;

[Authorize]
public sealed class ClipHub : Hub<IClipClient>
{
    public static string GroupName(Guid userId) => userId.ToString();

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.GetUserId() is { } userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
        }

        await base.OnConnectedAsync();
    }
}

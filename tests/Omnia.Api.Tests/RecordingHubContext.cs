using Microsoft.AspNetCore.SignalR;
using Omnia.Api.Hubs;
using Omnia.Shared.Contracts;
using Omnia.Shared.Realtime;

namespace Omnia.Api.Tests;

internal sealed class RecordingHubContext : IHubContext<ClipHub, IClipClient>
{
    public List<(string GroupName, ClipDto Clip)> Created { get; } = [];

    public List<(string GroupName, Guid ClipId)> Deleted { get; } = [];

    public IHubClients<IClipClient> Clients { get; }

    public IGroupManager Groups => throw new NotSupportedException();

    public RecordingHubContext() => Clients = new RecordingHubClients(this);

    private sealed class RecordingHubClients(RecordingHubContext owner) : IHubClients<IClipClient>
    {
        public IClipClient Group(string groupName) => new RecordingClient(owner, groupName);

        public IClipClient All => throw new NotSupportedException();

        public IClipClient Others => throw new NotSupportedException();

        public IClipClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();

        public IClipClient Client(string connectionId) => throw new NotSupportedException();

        public IClipClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();

        public IClipClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) =>
            throw new NotSupportedException();

        public IClipClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();

        public IClipClient OthersInGroup(string groupName) => throw new NotSupportedException();

        public IClipClient User(string userId) => throw new NotSupportedException();

        public IClipClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class RecordingClient(RecordingHubContext owner, string groupName) : IClipClient
    {
        public Task OnClipCreated(ClipDto clip)
        {
            owner.Created.Add((groupName, clip));
            return Task.CompletedTask;
        }

        public Task OnClipDeleted(Guid id)
        {
            owner.Deleted.Add((groupName, id));
            return Task.CompletedTask;
        }
    }
}

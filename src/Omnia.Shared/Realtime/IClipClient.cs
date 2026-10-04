using Omnia.Shared.Contracts;

namespace Omnia.Shared.Realtime;

public interface IClipClient
{
    Task OnClipCreated(ClipDto clip);
    Task OnClipDeleted(Guid id);
}

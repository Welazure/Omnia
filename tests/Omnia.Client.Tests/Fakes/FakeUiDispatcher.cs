using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}

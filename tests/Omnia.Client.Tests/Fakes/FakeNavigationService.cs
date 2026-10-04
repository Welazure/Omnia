using Omnia.Client.Services;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeNavigationService : INavigationService
{
    public ViewModelBase? CurrentPage { get; private set; }

    public event EventHandler? CurrentPageChanged;

    public int ShowWorkspaceCalls { get; private set; }

    public int ShowLoginCalls { get; private set; }

    public int ShowAccountCalls { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ShowWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        ShowWorkspaceCalls++;
        CurrentPage = new StubViewModel();
        CurrentPageChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public void ShowLogin()
    {
        ShowLoginCalls++;
        CurrentPage = new StubViewModel();
    }

    public void ShowAccount()
    {
        ShowAccountCalls++;
        CurrentPage = new StubViewModel();
    }
}

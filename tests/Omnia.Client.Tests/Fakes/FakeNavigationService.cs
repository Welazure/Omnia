using Omnia.Client.Services;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeNavigationService : INavigationService
{
    public ViewModelBase? CurrentPage { get; private set; }

    public event EventHandler? CurrentPageChanged;

    public int ShowWorkspaceCalls { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ShowWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        ShowWorkspaceCalls++;
        CurrentPage = new StubViewModel();
        CurrentPageChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public void ShowLogin() => CurrentPage = new StubViewModel();

    public void ShowAccount() => CurrentPage = new StubViewModel();
}

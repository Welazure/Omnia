using Omnia.Client.ViewModels;

namespace Omnia.Client.Services;

public interface INavigationService
{
    ViewModelBase? CurrentPage { get; }

    event EventHandler? CurrentPageChanged;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task ShowWorkspaceAsync(CancellationToken cancellationToken = default);

    void ShowLogin();

    void ShowAccount();
}

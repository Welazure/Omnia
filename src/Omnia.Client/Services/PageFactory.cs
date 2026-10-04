using Microsoft.Extensions.DependencyInjection;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Services;

public sealed class PageFactory(IServiceProvider serviceProvider) : IPageFactory
{
    public ViewModelBase CreateLogin() => serviceProvider.GetRequiredService<LoginViewModel>();

    public ViewModelBase CreateWorkspace() => new PlaceholderViewModel("Workspace");

    public ViewModelBase CreateAccount() => new PlaceholderViewModel("Account");
}

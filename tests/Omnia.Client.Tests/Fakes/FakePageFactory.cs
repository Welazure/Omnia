using Omnia.Client.Services;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakePageFactory : IPageFactory
{
    public ViewModelBase Login { get; set; } = new StubViewModel();

    public ViewModelBase Workspace { get; set; } = new StubViewModel();

    public ViewModelBase Account { get; set; } = new StubViewModel();

    public ViewModelBase CreateLogin() => Login;

    public ViewModelBase CreateWorkspace() => Workspace;

    public ViewModelBase CreateAccount() => Account;
}

using Omnia.Client.ViewModels;

namespace Omnia.Client.Services;

public interface IPageFactory
{
    ViewModelBase CreateLogin();

    ViewModelBase CreateWorkspace();

    ViewModelBase CreateAccount();
}

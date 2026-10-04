using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Omnia.Client.Services;

namespace Omnia.Client.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IApiClient apiClient;
    private readonly ITokenStore tokenStore;
    private readonly INavigationService navigation;

    public LoginViewModel(IApiClient apiClient, ITokenStore tokenStore, INavigationService navigation)
    {
        this.apiClient = apiClient;
        this.tokenStore = tokenStore;
        this.navigation = navigation;
    }

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRegisterMode { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public string SubmitLabel => IsRegisterMode ? "Sign up" : "Sign in";

    public string ToggleLabel => IsRegisterMode ? "Have an account? Sign in" : "No account? Sign up";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [RelayCommand]
    private async Task SubmitAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var response = IsRegisterMode
                ? await apiClient.RegisterAsync(Email, Password, cancellationToken)
                : await apiClient.LoginAsync(Email, Password, cancellationToken);

            if (response is null)
            {
                ErrorMessage = IsRegisterMode
                    ? "That email is already registered."
                    : "Invalid email or password.";
                return;
            }

            await tokenStore.SaveTokenAsync(response.Token, cancellationToken);
            await navigation.ShowWorkspaceAsync(cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Could not reach the server.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleMode() => IsRegisterMode = !IsRegisterMode;

    partial void OnIsRegisterModeChanged(bool value)
    {
        OnPropertyChanged(nameof(SubmitLabel));
        OnPropertyChanged(nameof(ToggleLabel));
    }

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
}

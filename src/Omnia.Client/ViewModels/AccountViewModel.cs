using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Omnia.Client.Services;

namespace Omnia.Client.ViewModels;

public partial class AccountViewModel : ViewModelBase
{
    private readonly IApiClient apiClient;
    private readonly ITokenStore tokenStore;
    private readonly IClipSyncService clipSyncService;
    private readonly INavigationService navigation;

    public AccountViewModel(
        IApiClient apiClient,
        ITokenStore tokenStore,
        IClipSyncService clipSyncService,
        INavigationService navigation)
    {
        this.apiClient = apiClient;
        this.tokenStore = tokenStore;
        this.clipSyncService = clipSyncService;
        this.navigation = navigation;
    }

    [ObservableProperty]
    public partial string? Email { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var user = await apiClient.GetMeAsync(cancellationToken);
            Email = user?.Email;
        }
        catch (ApiException)
        {
            ErrorMessage = "Could not load your account.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await clipSyncService.StopAsync();
        await tokenStore.ClearTokenAsync();
        navigation.ShowLogin();
    }

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
}

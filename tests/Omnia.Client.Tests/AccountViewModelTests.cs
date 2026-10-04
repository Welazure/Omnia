using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;
using Omnia.Client.ViewModels;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests;

public sealed class AccountViewModelTests
{
    [Fact]
    public async Task Load_SetsEmailFromApi()
    {
        var (viewModel, api, _, _, _) = Create();
        api.MeResult = new UserDto(Guid.NewGuid(), "user@example.com");

        await viewModel.LoadAsync();

        Assert.Equal("user@example.com", viewModel.Email);
        Assert.Null(viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Load_ApiFailure_ShowsError()
    {
        var (viewModel, api, _, _, _) = Create();
        api.ThrowOnGetMe = new ApiException("server down");

        await viewModel.LoadAsync();

        Assert.Null(viewModel.Email);
        Assert.NotNull(viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task SignOut_ClearsTokenStopsSyncAndNavigatesToLogin()
    {
        var (viewModel, _, tokenStore, clipSync, navigation) = Create();
        tokenStore.Token = "stored-token";

        await viewModel.SignOutCommand.ExecuteAsync(null);

        Assert.Null(tokenStore.Token);
        Assert.Equal(1, clipSync.StopCalls);
        Assert.Equal(1, navigation.ShowLoginCalls);
    }

    private static (AccountViewModel ViewModel, FakeApiClient Api, FakeTokenStore TokenStore, FakeClipSyncService ClipSync, FakeNavigationService Navigation) Create()
    {
        var api = new FakeApiClient();
        var tokenStore = new FakeTokenStore();
        var clipSync = new FakeClipSyncService();
        var navigation = new FakeNavigationService();
        var viewModel = new AccountViewModel(api, tokenStore, clipSync, navigation);
        return (viewModel, api, tokenStore, clipSync, navigation);
    }
}

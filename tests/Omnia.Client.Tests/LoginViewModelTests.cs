using CommunityToolkit.Mvvm.Input;
using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;
using Omnia.Client.ViewModels;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests;

public sealed class LoginViewModelTests
{
    private static readonly UserDto User = new(Guid.NewGuid(), "user@example.com");

    [Fact]
    public async Task Submit_Register_SavesTokenAndNavigatesToWorkspace()
    {
        var (viewModel, api, tokenStore, navigation) = Create();
        api.RegisterResult = new AuthResponse("register-token", User);
        viewModel.IsRegisterMode = true;
        viewModel.Email = "user@example.com";
        viewModel.Password = "password";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(1, api.RegisterCalls);
        Assert.Equal(0, api.LoginCalls);
        Assert.Equal("register-token", tokenStore.Token);
        Assert.Equal(1, navigation.ShowWorkspaceCalls);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Submit_Login_SavesTokenAndNavigatesToWorkspace()
    {
        var (viewModel, api, tokenStore, navigation) = Create();
        api.LoginResult = new AuthResponse("login-token", User);
        viewModel.Email = "user@example.com";
        viewModel.Password = "password";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(1, api.LoginCalls);
        Assert.Equal(0, api.RegisterCalls);
        Assert.Equal("login-token", tokenStore.Token);
        Assert.Equal(1, navigation.ShowWorkspaceCalls);
    }

    [Fact]
    public async Task Submit_Login_InvalidCredentials_ShowsErrorAndDoesNotStoreOrNavigate()
    {
        var (viewModel, api, tokenStore, navigation) = Create();
        api.LoginResult = null;
        viewModel.Email = "user@example.com";
        viewModel.Password = "wrong";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.ErrorMessage);
        Assert.Null(tokenStore.Token);
        Assert.Equal(0, navigation.ShowWorkspaceCalls);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Submit_Register_DuplicateEmail_ShowsErrorAndDoesNotStore()
    {
        var (viewModel, api, tokenStore, _) = Create();
        api.RegisterResult = null;
        viewModel.IsRegisterMode = true;
        viewModel.Email = "user@example.com";
        viewModel.Password = "password";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.ErrorMessage);
        Assert.Contains("already registered", viewModel.ErrorMessage);
        Assert.Null(tokenStore.Token);
    }

    [Fact]
    public async Task Submit_TransportFailure_ShowsReachabilityError()
    {
        var (viewModel, api, _, navigation) = Create();
        api.ThrowOnLogin = new ApiException("connection refused");
        viewModel.Email = "user@example.com";
        viewModel.Password = "password";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Could not reach the server.", viewModel.ErrorMessage);
        Assert.Equal(0, navigation.ShowWorkspaceCalls);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public void ToggleMode_FlipsRegisterModeAndLabels()
    {
        var (viewModel, _, _, _) = Create();

        Assert.Equal("Sign in", viewModel.SubmitLabel);

        viewModel.ToggleModeCommand.Execute(null);

        Assert.True(viewModel.IsRegisterMode);
        Assert.Equal("Sign up", viewModel.SubmitLabel);
    }

    [Fact]
    public void ErrorMessage_Set_RaisesHasError()
    {
        var (viewModel, _, _, _) = Create();

        Assert.False(viewModel.HasError);

        viewModel.ErrorMessage = "boom";

        Assert.True(viewModel.HasError);
    }

    private static (LoginViewModel ViewModel, FakeApiClient Api, FakeTokenStore TokenStore, FakeNavigationService Navigation) Create()
    {
        var api = new FakeApiClient();
        var tokenStore = new FakeTokenStore();
        var navigation = new FakeNavigationService();
        return (new LoginViewModel(api, tokenStore, navigation), api, tokenStore, navigation);
    }
}

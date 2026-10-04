using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;
using Omnia.Client.ViewModels;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests;

public sealed class ClipboardViewModelTests
{
    [Fact]
    public async Task Submit_NonEmptyInput_CreatesClipAndClearsInput()
    {
        var (viewModel, api, _, _, _) = Create();
        viewModel.Input = "hello";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(1, api.CreateCalls);
        Assert.Equal("hello", api.LastCreateContent);
        Assert.Equal(string.Empty, viewModel.Input);
        Assert.Null(viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Submit_EmptyInput_DoesNothing()
    {
        var (viewModel, api, _, _, _) = Create();
        viewModel.Input = "   ";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(0, api.CreateCalls);
    }

    [Fact]
    public async Task Submit_CreateFails_RestoresInputAndShowsError()
    {
        var (viewModel, api, _, _, _) = Create();
        api.ThrowOnCreate = new ApiException("server down");
        viewModel.Input = "hello";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("hello", viewModel.Input);
        Assert.NotNull(viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public void Constructor_LoadsExistingClips()
    {
        var (viewModel, _, _, _, _) = Create(clips: [Clip("a"), Clip("b")]);

        Assert.Equal(2, viewModel.Clips.Count);
        Assert.Equal("a", viewModel.Clips[0].Content);
        Assert.Equal("b", viewModel.Clips[1].Content);
    }

    [Fact]
    public void ClipsChanged_RebuildsListWithoutManualRefresh()
    {
        var (viewModel, _, clipSync, _, _) = Create(clips: [Clip("a")]);
        Assert.Single(viewModel.Clips);

        clipSync.Clips = [Clip("new"), Clip("a")];
        clipSync.RaiseClipsChanged();

        Assert.Equal(2, viewModel.Clips.Count);
        Assert.Equal("new", viewModel.Clips[0].Content);
    }

    [Fact]
    public async Task CopyClip_SetsClipboardTextAndShowsToast()
    {
        var gate = new TaskCompletionSource();
        var (viewModel, _, _, clipboard, _) = Create(delay: _ => gate.Task, clips: [Clip("hello")]);

        var copy = viewModel.CopyClipCommand.ExecuteAsync(viewModel.Clips[0]);

        Assert.Equal(1, clipboard.SetTextCalls);
        Assert.Equal("hello", clipboard.LastText);
        Assert.True(viewModel.IsToastVisible);
        Assert.Equal("Copied", viewModel.ToastMessage);

        gate.SetResult();
        await copy;

        Assert.False(viewModel.IsToastVisible);
        Assert.Null(viewModel.ToastMessage);
    }

    [Fact]
    public async Task CopyClip_SecondCopy_KeepsToastVisibleUntilLatestDelayCompletes()
    {
        var first = new TaskCompletionSource();
        var second = new TaskCompletionSource();
        var pending = new Queue<TaskCompletionSource>([first, second]);
        var (viewModel, _, _, _, _) = Create(delay: _ => pending.Dequeue().Task, clips: [Clip("a"), Clip("b")]);

        var firstCopy = viewModel.CopyClipCommand.ExecuteAsync(viewModel.Clips[0]);
        var secondCopy = viewModel.CopyClipCommand.ExecuteAsync(viewModel.Clips[1]);

        first.SetResult();
        await firstCopy;

        Assert.True(viewModel.IsToastVisible);

        second.SetResult();
        await secondCopy;

        Assert.False(viewModel.IsToastVisible);
    }

    [Fact]
    public async Task DeleteClip_CallsApiAndRemovesLocally()
    {
        var first = Clip("first");
        var second = Clip("second");
        var (viewModel, api, _, _, _) = Create(clips: [first, second]);

        await viewModel.DeleteClipCommand.ExecuteAsync(viewModel.Clips.First(item => item.Id == first.Id));

        Assert.Equal(1, api.DeleteCalls);
        Assert.Equal(first.Id, api.LastDeleteId);
        Assert.DoesNotContain(viewModel.Clips, item => item.Id == first.Id);
        Assert.Contains(viewModel.Clips, item => item.Id == second.Id);
    }

    [Fact]
    public async Task DeleteClip_ServerFailure_ReconcilesFromServer()
    {
        var first = Clip("first");
        var second = Clip("second");
        var (viewModel, api, _, _, _) = Create(clips: [first, second]);
        api.ThrowOnDelete = new ApiException("server down");

        await viewModel.DeleteClipCommand.ExecuteAsync(viewModel.Clips.First(item => item.Id == first.Id));

        Assert.Equal(2, viewModel.Clips.Count);
    }

    [Fact]
    public void Status_Connected_SetsConnectedIndicator()
    {
        var (viewModel, _, clipSync, _, _) = Create();

        clipSync.Status = ConnectionStatus.Connected;

        Assert.True(viewModel.IsConnected);
        Assert.False(viewModel.IsReconnecting);
        Assert.Equal(ConnectionStatus.Connected, viewModel.Status);
    }

    [Fact]
    public void Status_Reconnecting_SetsReconnectingIndicator()
    {
        var (viewModel, _, clipSync, _, _) = Create();

        clipSync.Status = ConnectionStatus.Reconnecting;

        Assert.True(viewModel.IsReconnecting);
        Assert.False(viewModel.IsConnected);
    }

    [Fact]
    public void Status_OtherStatuses_ShowNeitherIndicator()
    {
        var (viewModel, _, clipSync, _, _) = Create();

        clipSync.Status = ConnectionStatus.Disconnected;

        Assert.False(viewModel.IsConnected);
        Assert.False(viewModel.IsReconnecting);
    }

    [Fact]
    public void OpenAccount_NavigatesToAccount()
    {
        var (viewModel, _, _, _, navigation) = Create();

        viewModel.OpenAccountCommand.Execute(null);

        Assert.Equal(1, navigation.ShowAccountCalls);
    }

    private static (ClipboardViewModel ViewModel, FakeApiClient Api, FakeClipSyncService ClipSync, FakeClipboardService Clipboard, FakeNavigationService Navigation) Create(
        ToastDelay? delay = null,
        params ClipDto[] clips)
    {
        var api = new FakeApiClient();
        var clipSync = new FakeClipSyncService { Clips = clips };
        var clipboard = new FakeClipboardService();
        var navigation = new FakeNavigationService();
        var viewModel = new ClipboardViewModel(
            api,
            clipSync,
            clipboard,
            navigation,
            delay ?? (_ => Task.CompletedTask),
            new FakeUiDispatcher());
        return (viewModel, api, clipSync, clipboard, navigation);
    }

    private static ClipDto Clip(string content) => new(Guid.NewGuid(), content, null, DateTimeOffset.UtcNow);
}

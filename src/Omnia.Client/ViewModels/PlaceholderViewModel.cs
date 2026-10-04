namespace Omnia.Client.ViewModels;

public sealed class PlaceholderViewModel(string title) : ViewModelBase
{
    public string Title { get; } = title;
}

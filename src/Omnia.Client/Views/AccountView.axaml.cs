using Avalonia.Controls;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Views;

public partial class AccountView : UserControl
{
    public AccountView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => _ = (DataContext as AccountViewModel)?.LoadAsync();
    }
}

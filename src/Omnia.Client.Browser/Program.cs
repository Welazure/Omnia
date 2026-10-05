using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Microsoft.Extensions.DependencyInjection;
using Omnia.Client;
using Omnia.Client.Browser.Services;
using Omnia.Client.Services;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        App.ConfigureClient = options => options.ForceWebSockets = true;
        App.ConfigurePlatformServices = services => services.AddSingleton<ITokenStore, BrowserTokenStore>();

        // The browser cannot read the clipboard without a user gesture, so it degrades to manual.
        App.ClipboardCapability = ClipboardCapability.Manual;

        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}

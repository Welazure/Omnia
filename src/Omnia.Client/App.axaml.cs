using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Omnia.Client.Services;
using Omnia.Client.ViewModels;
using Omnia.Client.Views;

namespace Omnia.Client;

public partial class App : Application
{
    private ServiceProvider? services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        collection.AddOmniaClient();
        services = collection.BuildServiceProvider();

        var shell = services.GetRequiredService<ShellViewModel>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<App>();
        var clipboard = services.GetRequiredService<AvaloniaClipboardService>();
        _ = StartShellAsync(shell, logger);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow
            {
                DataContext = shell
            };
            desktop.MainWindow = window;
            clipboard.Attach(window);
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () =>
            {
                var view = new MainView { DataContext = shell };
                clipboard.Attach(view);
                return view;
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var view = new MainView
            {
                DataContext = shell
            };
            singleViewPlatform.MainView = view;
            clipboard.Attach(view);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task StartShellAsync(ShellViewModel shell, ILogger logger)
    {
        try
        {
            await shell.StartAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Client startup failed.");
            shell.ShowLogin();
        }
    }
}

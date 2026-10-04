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
        _ = StartShellAsync(shell, logger);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = shell
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = shell };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = shell
            };
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

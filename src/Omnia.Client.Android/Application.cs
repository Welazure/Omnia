using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Omnia.Client.Services;

namespace Omnia.Client.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            // Android clipboard reads are prompt-gated and may fail, so sync is best effort.
            App.ClipboardCapability = ClipboardCapability.BestEffort;
            return base.CustomizeAppBuilder(builder)
            .WithInterFont();
        }
    }
}

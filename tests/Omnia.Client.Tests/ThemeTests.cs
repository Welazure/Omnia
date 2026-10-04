namespace Omnia.Client.Tests;

public sealed class ThemeTests
{
    [Fact]
    public void App_UsesDarkVariantAndBlackWhiteResources()
    {
        var xaml = File.ReadAllText(TestPaths.ClientFile("App.axaml"));

        Assert.Contains("RequestedThemeVariant=\"Dark\"", xaml);
        Assert.Contains("#000000", xaml);
        Assert.Contains("#FFFFFF", xaml);
    }

    [Theory]
    [InlineData("Views/ClipboardView.axaml")]
    [InlineData("Views/AccountView.axaml")]
    public void View_HasNoAppBarFabOrBottomNav(string relativePath)
    {
        var xaml = File.ReadAllText(TestPaths.ClientFile(relativePath));

        foreach (var chrome in new[] { "NavigationView", "TabControl", "TabStrip", "BottomNavigation", "FloatingAction", "AppBar", "CommandBar" })
        {
            Assert.DoesNotContain(chrome, xaml);
        }
    }

    [Fact]
    public void ClipboardView_MapsConnectedToGreenAndReconnectingToOrange()
    {
        var xaml = File.ReadAllText(TestPaths.ClientFile("Views/ClipboardView.axaml"));

        Assert.Contains("#4CAF50", xaml);
        Assert.Contains("IsConnected", xaml);
        Assert.Contains("#FFA500", xaml);
        Assert.Contains("IsReconnecting", xaml);
    }

    [Fact]
    public void ClipboardView_UsesBlackBackground()
    {
        var xaml = File.ReadAllText(TestPaths.ClientFile("Views/ClipboardView.axaml"));

        Assert.Contains("#000000", xaml);
    }
}

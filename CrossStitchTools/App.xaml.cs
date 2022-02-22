using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CrossStitchTools;

public partial class App : Application
{
    public static NavigationView RootNavigationView { get; set; } = null!;
    public static Frame RootFrame { get; set; } = null!;
    public static MainWindow Window { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();
        Window.Activate();
    }
}
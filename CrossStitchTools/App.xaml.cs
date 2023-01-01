using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Utilities;

namespace CrossStitchTools;

public partial class App : Application
{
    public static AppConfig AppConfig { get; private set; } = null!;
    public static NavigationView RootNavigationView { get; set; } = null!;
    public static Frame RootFrame { get; set; } = null!;

    public App()
    {
        InitializeComponent();
        CurrentContext.App = this;
        CurrentContext.Title = nameof(CrossStitchTools);
        AppContext.InitializeConfigurationContainer();
        if (AppContext.LoadConfiguration() is not { } appConfigurations
#if FIRST_TIME
        || true
#endif
           )
            AppConfig = new AppConfig();
        else
            AppConfig = appConfigurations;
    }

    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        CurrentContext.Window = new MainWindow();
        AppHelper.Initialize(AppHelper.PredetermineEstimatedWindowSize());
    }
}

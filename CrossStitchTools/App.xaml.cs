using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace CrossStitchTools;

public partial class App : Application
{
    public static AppConfig AppConfig { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        CurrentContext.Title = nameof(CrossStitchTools);
        AppContext.InitializeConfigurationContainer();
        AppConfig = AppContext.LoadConfiguration() is not { } appConfigurations ?
#if FIRST_TIME
        || true
#endif
            new() : appConfigurations;
    }

    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = new MainWindow();
        AppHelper.Initialize(new()
        {
            Size = WindowHelper.EstimatedWindowSize(),
            TitleBarType = TitleBarHelper.TitleBarType.AppWindow
        });
    }
}

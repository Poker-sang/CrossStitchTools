using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace CrossStitchTools;

public partial class App : Application
{
    public static AppConfig AppConfig { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        CurrentContext.Title = "十字绣工具";
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
        var window = new MainWindow();
        window.Initialize(new()
        {
            Size = WindowHelper.EstimatedWindowSize(),
            TitleBarType = TitleBarType.AppWindow,
            BackdropType = BackdropType.MicaAlt
        });
        window.SetAppWindowTheme(AppConfig.Theme);
        window.Activate();
    }
}

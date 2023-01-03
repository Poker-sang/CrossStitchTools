using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using WinUI3Utilities;

namespace CrossStitchTools.Views;

public partial class SettingsPage : Page
{
    public SettingsPage() => InitializeComponent();

    private void OnThemeChecked(object sender, RoutedEventArgs e)
    {
        var selectedTheme = sender.GetTag<int>() switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (CurrentContext.Window.Content is FrameworkElement rootElement)
            rootElement.RequestedTheme = selectedTheme;

        CurrentContext.App.Resources["WindowCaptionForeground"] = selectedTheme switch
        {
            ElementTheme.Dark => Colors.White,
            ElementTheme.Light => Colors.Black,
            _ => CurrentContext.App.RequestedTheme is ApplicationTheme.Dark ? Colors.White : Colors.Black
        };

        App.AppConfig.Theme = (int)selectedTheme;

        AppContext.SaveConfiguration(App.AppConfig);
    }
}

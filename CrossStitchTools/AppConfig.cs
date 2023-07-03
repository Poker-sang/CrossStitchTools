using Microsoft.UI.Xaml;
using WinUI3Utilities.Attributes;

namespace CrossStitchTools;

[GenerateConstructor]
public partial record AppConfig
{
    public ElementTheme Theme { get; set; } = ElementTheme.Light;

    public AppConfig() { }
}

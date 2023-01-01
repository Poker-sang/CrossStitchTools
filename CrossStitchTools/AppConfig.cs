using WinUI3Utilities.Attributes;

namespace CrossStitchTools;

[GenerateConstructor]
public partial record AppConfig
{
    public int Theme { get; set; }

    public AppConfig() { }
}

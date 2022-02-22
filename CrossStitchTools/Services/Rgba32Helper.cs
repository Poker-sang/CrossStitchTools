using SixLabors.ImageSharp.PixelFormats;

namespace CrossStitchTools.Services;

public static class Rgba32Helper
{
    public static string GetName(this Rgba32 rgba32) => $"#{rgba32.R:X2}{rgba32.G:X2}{rgba32.B:X2}";
}
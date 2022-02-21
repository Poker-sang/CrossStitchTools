using CrossStitchTools.Models;

namespace CrossStitchTools.Services;

internal class ColorGroupCountComparer : System.Collections.Generic.IComparer<ColorGroup>
{
    public int Compare(ColorGroup? a, ColorGroup? b) => a!.Count.CompareTo(b!.Count);
}

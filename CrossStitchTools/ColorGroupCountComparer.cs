using CrossStitchTools.Models;

namespace CrossStitchTools;

internal class ColorGroupCountComparer : System.Collections.Generic.IComparer<ColorGroup>
{
    public int Compare(ColorGroup? a, ColorGroup? b) => a!.Count.CompareTo(b!.Count);
}

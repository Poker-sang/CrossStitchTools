using Microsoft.UI.Xaml.Media;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Numerics;

namespace CrossStitchTools.Models;

public class ColorGroup
{
    public ColorGroup(Rgba32 represent)
    {
        Represent = represent;
        Set.Add(Represent);
    }

    public static bool RepresentEqual(ColorGroup? a, ColorGroup? b)
    {
        if (a is null || b is null)
            return false;
        return a.Represent == b.Represent;
    }

    public Rgba32 Represent { get; set; }

    public HashSet<Rgba32> Set { get; } = new();

    public int Count;

    public void Merge(ColorGroup group)
    {
        Set.Add(group.Represent);
        Count += group.Count;
    }

    private static int Pow2(int a) => a * a;

    public int GetErr(Rgba32 color) => (int)Vector4.DistanceSquared(color.ToVector4(), Represent.ToVector4());

    public string Name => $"{Represent.R:X2}{Represent.G:X2}{Represent.B:X2}";

    public Brush Color => new SolidColorBrush(Windows.UI.Color.FromArgb(Represent.A, Represent.R, Represent.G, Represent.B));
}

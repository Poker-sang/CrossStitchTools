using Microsoft.UI.Xaml.Media;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CrossStitchTools.Models;

public class ColorGroup
{
    public ColorGroup(Rgba32 represent)
    {
        Represent = represent;
        _ = Set.Add(Represent);
    }

    public static bool RepresentEqual(ColorGroup? a, ColorGroup? b)
    {
        return a is not null && b is not null && a.Represent == b.Represent;
    }

    public Rgba32 Represent { get; set; }

    public HashSet<Rgba32> Set { get; } = new();

    public int Count;

    public void Merge(ColorGroup group)
    {
        _ = Set.Add(group.Represent);
        Count += group.Count;
    }

    private static Vector3 ToVector3(Rgba32 color) => new(color.R, color.G, color.B);

    public int GetErr(Rgba32 color)
    {
        return (int)Vector3.Distance(ToVector3(color), ToVector3(Represent));
    }

    public string Name => $"{Represent.R:X2}{Represent.G:X2}{Represent.B:X2}";

    public Brush Color => new SolidColorBrush(Windows.UI.Color.FromArgb(Represent.A, Represent.R, Represent.G, Represent.B));
}

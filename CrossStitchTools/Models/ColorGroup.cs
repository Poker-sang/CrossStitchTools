using Microsoft.UI.Xaml.Media;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;

namespace CrossStitchTools.Models;

public class ColorGroup
{
    public ColorGroup(Rgba32 represent)
    {
        Represent = represent;
        Set.Add(Represent);
    }

    public Rgba32 Represent;
    public readonly HashSet<Rgba32> Set = new();

    public int Count;

    public void Merge(ColorGroup group)
    {
        Set.Add(group.Represent);
        Count += group.Count;
    }

    private static int Pow2(int a) => a * a;

    public int GetErr(Rgba32 color) => Math.Abs(Pow2(color.R - Represent.R) + Pow2(color.G - Represent.G) + Pow2(color.B - Represent.B));

    public string Name => $"#{Represent.R:X2}{Represent.G:X2}{Represent.B:X2}";
    public Brush Color => new SolidColorBrush(Windows.UI.Color.FromArgb(Represent.A, Represent.R, Represent.G, Represent.B));
    public ColorGroup This => this;
}
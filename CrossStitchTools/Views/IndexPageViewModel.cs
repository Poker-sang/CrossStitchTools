using System.Collections.Generic;
using System.IO;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CrossStitchTools.Enums;
using CrossStitchTools.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Point = Windows.Foundation.Point;

namespace CrossStitchTools.Views;

public partial class IndexPageViewModel : ObservableObject
{
    public ListView ListView = null!;

    public ImageDisplaying ImageDisplaying
    {
        get => _imageDisplaying;
        set
        {
            if (value is not ImageDisplaying.SelectColor)
                ListView.SelectedIndex = -1;
            _imageDisplaying = value;
        }
    }

    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private bool _isActed;

    public Point LastPoint;
    public (double X, double Y, Rgba32 Color) CurrentCoordinate;
    private ImageDisplaying _imageDisplaying;
    public ColorGroup LastSelectedColor = null!;
    public ColorGroup? LastSelectedSubColor;
    public Image<Rgba32>? OriginImage;
    public Image<Rgba32>? AfterImage;
    public Image<Rgba32>? SelectColorImage;
    public readonly BitmapImage OriginBitmap = new();
    public readonly BitmapImage AfterBitmap = new();
    public readonly BitmapImage SelectColorBitmap = new();
    public int ImageWidth => OriginImage!.Width;
    public int ImageHeight => OriginImage!.Height;

    private int _pixelLength = 1;
    public int PixelLength
    {
        get => _pixelLength;
        set
        {
            _pixelLength = value;
            OnPropertyChanged(nameof(PixelActualLength));
        }
    }

    [ObservableProperty] private List<ColorGroup> _itemList = null!;

    public double PixelActualLength => BitmapScale * PixelLength;

    public float BitmapScale
    {
        get => Scale.X;
        set
        {
            Scale = new(value, value, 1);
            OnPropertyChanged();
            OnPropertyChanged(nameof(PixelActualLength));
        }
    }

    [ObservableProperty] private Vector3 _scale;
    [ObservableProperty] private double _bitmapCanvasLeft;
    [ObservableProperty] private double _bitmapCanvasTop;

    public (double Left, double Top) BitmapPosition
    {
        get => (BitmapCanvasLeft, BitmapCanvasTop);
        set
        {
            BitmapCanvasLeft = value.Left;
            BitmapCanvasTop = value.Top;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CrossStitchTools.Enums;
using CrossStitchTools.Interfaces;
using CrossStitchTools.Models;
using CrossStitchTools.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;
using Point = Windows.Foundation.Point;

namespace CrossStitchTools.Views;

[INotifyPropertyChanged]
public sealed partial class IndexPage : Page, ITypeGetter
{
    public IndexPage() => InitializeComponent();

    public static Type TypeGetter => typeof(IndexPage);

    private ImageDisplaying ImageDisplaying
    {
        get => _imageDisplaying;
        set
        {
            if (value is not ImageDisplaying.SelectColor)
                ListView.SelectedIndex = -1;
            _imageDisplaying = value;
        }
    }

    private MemoryStream? _memoryStream;
    private MemoryStream? MemoryStream
    {
        get => _memoryStream;
        set
        {
            _memoryStream = value;
            BSave.IsEnabled = BAction.IsEnabled = value is not null;
            BToggle.IsEnabled = false;
            BAction.Content = "Action";
        }
    }

    private Point _lastPoint;
    private (double X, double Y, Rgba32 Color) _currentCoordinate;
    private ImageDisplaying _imageDisplaying;
    private ColorGroup _lastSelectedColor = null!;
    private ColorGroup? _lastSelectedSubColor;
    private Image<Rgba32>? _originImage;
    private Image<Rgba32>? _afterImage;
    private Image<Rgba32>? _selectColorImage;
    private readonly BitmapImage _originBitmap = new();
    private readonly BitmapImage _afterBitmap = new();
    private readonly BitmapImage _selectColorBitmap = new();
    private int ImageWidth => _originImage!.Width;
    private int ImageHeight => _originImage!.Height;

    private int _pixelLength = 1;
    private int PixelLength
    {
        get => _pixelLength;
        set
        {
            _pixelLength = value;
            OnPropertyChanged(nameof(PixelActualLength));
        }
    }

    [ObservableProperty] private List<ColorGroup> _itemList = null!;

    private double PixelActualLength => BitmapScale * PixelLength;

    private float BitmapScale
    {
        get => IBitmap.Scale.X;
        set
        {
            IBitmap.Scale = new Vector3(value, value, 1);
            OnPropertyChanged(nameof(PixelActualLength));
        }
    }

    private (double Left, double Top) BitmapPosition
    {
        get => (Canvas.GetLeft(IBitmap), Canvas.GetTop(IBitmap));
        set
        {
            Canvas.SetLeft(IBitmap, value.Left);
            Canvas.SetTop(IBitmap, value.Top);
        }
    }

    #region 事件处理

    private async void BOpenClick(object sender, RoutedEventArgs e)
    {
        if (await FileSystemHelper.GetStorageFile() is { } file)
        {
            if (MemoryStream is not null)
            {
                MemoryStream.Close();
                await MemoryStream.DisposeAsync();
            }

            MemoryStream = new();
            await MemoryStream.WriteAsync(await File.ReadAllBytesAsync(file.Path));
            MemoryStream.Position = 0;
            await _originBitmap.SetSourceAsync(MemoryStream.AsRandomAccessStream());
            MemoryStream.Position = 0;
            _originImage = await Image.LoadAsync<Rgba32>(MemoryStream);
            PixelLength = 1;
            TbPixel.Text = $"{ImageWidth}x{ImageHeight}";
            SetImageSource(_originBitmap, ImageDisplaying.Origin, false);
            BAction.IsEnabled = true;
            BAction.Content = "Action";
            LHorizontal.X2 = ImageWidth;
            LVertical.Y2 = ImageHeight;
            RectSizeChanged();
        }
    }

    private async void BActionClick(object sender, RoutedEventArgs e)
    {
        if (MemoryStream is null || _originImage is null)
            return;
        BAction.IsEnabled = false;
        BAction.Content = "Acted";
        BToggle.IsEnabled = true;

        _lastSelectedColor = null!;
        _lastSelectedSubColor = null;
        _afterImage?.Dispose();
        _afterImage = null;
        _selectColorImage?.Dispose();
        _selectColorImage = null;
        GC.Collect();

        var colorErr = (int)(NbColor.Value is double.NaN ? 50 : NbColor.Value);
        var countErr = (int)(NbCount.Value is double.NaN ? 50 : NbCount.Value);
        PixelLength = (int)(NbPixelLength.Value is double.NaN ? 8 : NbPixelLength.Value);

        _afterImage = new Image<Rgba32>(ImageWidth, ImageHeight);
        var dict = new Dictionary<Rgba32, int>();
        for (var y = 0; y < ImageHeight; y += PixelLength)
            for (var x = 0; x < ImageWidth; x += PixelLength)
            {
                var tempColor = _originImage[x, y];
                dict[tempColor] = dict.ContainsKey(tempColor) ? dict[tempColor] + 1 : 1;
            }

        //只保留数量够多的颜色
        var itemList = new List<ColorGroup>();
        foreach (var (key, value) in dict)
            if (value > countErr)
                itemList.Add(new ColorGroup(key));

        //计数，第一次合并
        for (var y = 0; y < ImageHeight; y += PixelLength)
            for (var x = 0; x < ImageWidth; x += PixelLength)
            {
                var tempColor = _originImage[x, y];
                var min = 0;
                for (var k = 1; k < itemList.Count; ++k)
                    if (itemList[min].GetErr(tempColor) > itemList[k].GetErr(tempColor))
                        min = k;
                ++itemList[min].Count;
            }

        //第二次合并，只保留色差够大的颜色（数量少的颜色被剔除）
        itemList.Sort(new ColorGroupCountComparer());
        if (itemList.Count > 2)
            for (var i = 0; i < itemList.Count;)
            {
                var leastErrIndex = -1;
                var leastErr = colorErr + 1;
                for (var j = i + 1; j < itemList.Count; ++j)
                {
                    var errTemp = itemList[j].GetErr(itemList[i].Represent);
                    if (errTemp < colorErr)
                        if (leastErr > errTemp)
                        {
                            leastErrIndex = j;
                            leastErr = errTemp;
                        }
                }

                if (leastErrIndex != -1)
                {
                    itemList[leastErrIndex].Merge(itemList[i]);
                    itemList.RemoveAt(i);
                    itemList.Sort(new ColorGroupCountComparer());
                }
                else ++i;
            }

        ItemList = itemList;
        TbColor.Text = $"颜色数：{itemList.Count}";

        for (var y = 0; y < ImageHeight; y += PixelLength)
            for (var x = 0; x < ImageWidth; x += PixelLength)
            {
                var tempColor = _originImage[x, y];
                var min = 0;
                for (var k = 1; k < itemList.Count; ++k)
                    if (itemList[min].GetErr(tempColor) > itemList[k].GetErr(tempColor))
                        min = k;
                for (var xi = 0; xi < PixelLength; ++xi)
                    for (var yi = 0; yi < PixelLength; ++yi)
                        _afterImage[x + xi, y + yi] = itemList[min].Represent;
                ++itemList[min].Count;
            }

        await using var ms = new MemoryStream();
        await _afterImage.SaveAsync(ms, new PngEncoder());
        ms.Position = 0;
        await _afterBitmap.SetSourceAsync(ms.AsRandomAccessStream());
        AimChanged(_lastPoint);
    }

    private void BToggleClick(object sender, TappedRoutedEventArgs e)
    {
        if (BToggle.IsChecked is true)
            SetImageSource(_afterBitmap, ImageDisplaying.After, false);
        else SetImageSource(_originBitmap, ImageDisplaying.Origin, false);
    }

    private async void BSaveClick(object sender, TappedRoutedEventArgs e)
    {
        if (_originImage is null || _afterImage is null || _selectColorImage is null)
            return;
        if (await FileSystemHelper.GetStorageFolder() is { } folder)
        {
            var saveAsync = ImageDisplaying switch
            {
                ImageDisplaying.Origin => _originImage.SaveAsync(folder.Path + "\\Origin.png"),
                ImageDisplaying.After => _afterImage.SaveAsync(folder.Path + "\\After.png"),
                ImageDisplaying.SelectColor => _selectColorImage.SaveAsync(folder.Path + $"\\SelectColor {_lastSelectedColor.Represent.GetName()}.png"),
                _ => throw new ArgumentOutOfRangeException()
            };
            await saveAsync;
        }
    }

    private async void ColorSelect(object sender, TappedRoutedEventArgs e)
    {
        if (((StackPanel)sender).Tag is ColorGroup colorGroup)
            await SelectColor(colorGroup);
    }

    private async void SubColorSelect(object sender, RightTappedRoutedEventArgs e)
    {
        if (((StackPanel)sender).Tag is ColorGroup colorGroup && colorGroup != _lastSelectedColor)
            if (_lastSelectedSubColor == colorGroup)
                await SelectNewColor(_lastSelectedColor, null);
            else await SelectNewColor(_lastSelectedColor, colorGroup);
    }

    private async void IBitmapOnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        switch (ImageDisplaying)
        {
            case ImageDisplaying.After:
                {
                    if (ItemList.Find(color => color.Set.Contains(_currentCoordinate.Color)) is { } colorGroup)
                    {
                        ListView.SelectedIndex = ItemList.IndexOf(colorGroup);
                        await SelectColor(colorGroup);
                    }
                    break;
                }
            case ImageDisplaying.SelectColor when _afterImage is null:
                return;
            case ImageDisplaying.SelectColor:
                SetImageSource(_afterBitmap, ImageDisplaying.After, true);
                break;
            case ImageDisplaying.Origin:
            default: break;
        }
    }

    private void CanvasOnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(Canvas);
        var (left, top) = BitmapPosition;
        if (currentPoint.Properties.IsLeftButtonPressed)
            BitmapPosition = (currentPoint.Position.X - _lastPoint.X + left, currentPoint.Position.Y - _lastPoint.Y + top);
        AimChanged(_lastPoint = currentPoint.Position);
    }

    private void CanvasOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        BitmapScale += e.GetCurrentPoint(Canvas).Properties.MouseWheelDelta / 10000f;
        AimChanged(_lastPoint);
    }

    private void CanvasOnSizeChanged(object sender, SizeChangedEventArgs e) => RectSizeChanged();

    #endregion

    #region 操作

    private async Task SelectColor(ColorGroup color)
    {
        if (_lastSelectedColor == color)
        {
            if (_selectColorImage is null)
                return;
            if (ImageDisplaying is ImageDisplaying.SelectColor)
                SetImageSource(_afterBitmap, ImageDisplaying.After, true);
            else SetImageSource(_selectColorBitmap, ImageDisplaying.SelectColor, true);
        }
        else await SelectNewColor(color, _lastSelectedSubColor);
    }

    private async Task SelectNewColor(ColorGroup newColor, ColorGroup? subColor)
    {
        if (_afterImage is null)
            return;
        _lastSelectedColor = newColor;
        _lastSelectedSubColor = subColor;

        _selectColorImage = new Image<Rgba32>(_afterImage.Width, _afterImage.Height);
        for (var y = 0; y < _afterImage.Height; ++y)
            for (var x = 0; x < _afterImage.Width; ++x)
                _selectColorImage[x, y] = 0 switch
                {
                    0 when newColor.Set.Contains(_afterImage[x, y]) => Color.Red,
                    0 when subColor?.Set.Contains(_afterImage[x, y]) ?? false => Color.Black,
                    _ => Color.White
                };
        await using var ms = new MemoryStream();
        await _selectColorImage.SaveAsync(ms, new PngEncoder());
        ms.Position = 0;
        await _selectColorBitmap.SetSourceAsync(ms.AsRandomAccessStream());
        SetImageSource(_selectColorBitmap, ImageDisplaying.SelectColor, true);
    }

    private void SetImageSource(ImageSource source, ImageDisplaying imageDisplaying, bool keepProperty)
    {
        if (!keepProperty)
        {
            BitmapScale = 1;
            BitmapPosition = (0, 0);
        }
        IBitmap.Source = source;
        ImageDisplaying = imageDisplaying;
    }

    private void RectSizeChanged() => RgCanvas.Rect = new(0, 0, Canvas.ActualWidth, Canvas.ActualHeight);

    private void AimChanged(Point currentPoint)
    {
        var (left, top) = BitmapPosition;

        var xBitmap = currentPoint.X / BitmapScale - left;
        var yBitmap = currentPoint.Y / BitmapScale - top;
        var xPixel = Math.Min((int)((currentPoint.X - left) / (BitmapScale * PixelLength)) * PixelLength, ImageWidth - 1);
        var yPixel = Math.Min((int)((currentPoint.Y - top) / (BitmapScale * PixelLength)) * PixelLength, ImageHeight - 1);

        LVertical.X1 = LVertical.X2 = (xPixel + 0.5 * PixelLength) * BitmapScale + left;
        LHorizontal.Y1 = LHorizontal.Y2 = (yPixel + 0.5 * PixelLength) * BitmapScale + top;

        _currentCoordinate = ImageDisplaying switch
        {
            ImageDisplaying.Origin when _originImage is not null => (xBitmap, yBitmap, _originImage[xPixel, yPixel]),
            ImageDisplaying.After when _afterImage is not null => (xBitmap, yBitmap, _afterImage[xPixel, yPixel]),
            ImageDisplaying.SelectColor when _selectColorImage is not null => (xBitmap, yBitmap, _selectColorImage[xPixel, yPixel]),
            _ => throw new ArgumentOutOfRangeException()
        };

        TbPointer.Text = $"Pointer:\nX:{xBitmap:F2}\nY:{yBitmap:F2}\nPixel:\nX:{xPixel / PixelLength}\nY:{yPixel / PixelLength}\n{_currentCoordinate.Color.GetName()}";
    }

    #endregion
}
using CommunityToolkit.Mvvm.ComponentModel;
using CrossStitchTools.Enums;
using CrossStitchTools.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using WinUI3Utilities;
using Image = SixLabors.ImageSharp.Image;
using Point = Windows.Foundation.Point;

namespace CrossStitchTools.Views;

[INotifyPropertyChanged]
public sealed partial class IndexPage : Page
{
    private const double Increment = 0.01;

    public IndexPage()
    {
        InitializeComponent();
        _vm.ListView = ListView;
    }

    private readonly IndexPageViewModel _vm = new();

    #region 事件处理

    private void BThemeChange(object sender, RoutedEventArgs e)
    {
        App.AppConfig.Theme = App.AppConfig.Theme switch
        {
            ElementTheme.Light => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Light,
            _ => ThrowHelper.ArgumentOutOfRange<ElementTheme, ElementTheme>(App.AppConfig.Theme)
        };
        CurrentContext.Window.SetAppWindowTheme(App.AppConfig.Theme);
        AppContext.SaveConfiguration(App.AppConfig);
    }

    private async void BOpenClick(object sender, TappedRoutedEventArgs e)
    {
        if (await PickerHelper.PickSingleFileAsync() is { } file)
        {
            _vm.IsLoaded = _vm.IsActed = false;
            var imageSource = await StorageFile.GetFileFromPathAsync(file.Path);
            using var randomAccessStreamWithContentType = await imageSource.OpenReadAsync();
            await _vm.OriginBitmap.SetSourceAsync(randomAccessStreamWithContentType);
            _vm.OriginImage = await Image.LoadAsync<Rgba32>(file.Path);
            _vm.IsLoaded = true;

            _vm.PixelLength = 1;
            TbPixel.Text = $"{_vm.ImageWidth}x{_vm.ImageHeight}";
            SetImageSource(_vm.OriginBitmap, ImageDisplaying.Origin, false);
            RectSizeChanged();
        }
    }

    private async void BActionClick(object sender, TappedRoutedEventArgs e)
    {
        if (_vm.OriginImage is null)
            return;

        _vm.IsLoaded = false;

        _vm.LastSelectedColor = null!;
        _vm.LastSelectedSubColor = null;
        _vm.AfterImage?.Dispose();
        _vm.AfterImage = null;
        _vm.SelectColorImage?.Dispose();
        _vm.SelectColorImage = null;
        GC.Collect();

        var colorErr = (int)(NbColor.Value is double.NaN ? 50 : NbColor.Value);
        var countErr = (int)(NbCount.Value is double.NaN ? 50 : NbCount.Value);
        _vm.PixelLength = (int)(NbPixelLength.Value is double.NaN ? 8 : NbPixelLength.Value);

        _vm.AfterImage = new(_vm.ImageWidth, _vm.ImageHeight);
        var dict = new Dictionary<Rgba32, int>();
        for (var y = 0; y < _vm.ImageHeight; y += _vm.PixelLength)
            for (var x = 0; x < _vm.ImageWidth; x += _vm.PixelLength)
            {
                var tempColor = _vm.OriginImage[x, y];
                dict[tempColor] = dict.TryGetValue(tempColor, out var value) ? value + 1 : 1;
            }

        //只保留数量够多的颜色
        var itemList = new List<ColorGroup>();
        foreach (var (key, value) in dict)
            if (value > countErr)
                itemList.Add(new(key));

        //计数，第一次合并
        for (var y = 0; y < _vm.ImageHeight; y += _vm.PixelLength)
            for (var x = 0; x < _vm.ImageWidth; x += _vm.PixelLength)
            {
                var tempColor = _vm.OriginImage[x, y];
                var min = 0;
                for (var k = 1; k < itemList.Count; ++k)
                    if (itemList[min].GetErr(tempColor) > itemList[k].GetErr(tempColor))
                        min = k;
                ++itemList[min].Count;
            }

        //第二次合并，只保留色差够大的颜色（数量少的颜色被剔除）
        itemList.Sort((a, b) => a.Count.CompareTo(b.Count));
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
                    itemList.Sort((a, b) => a.Count.CompareTo(b.Count));
                }
                else
                    ++i;
            }

        _vm.ItemList = itemList;
        TbColor.Text = $"颜色数：{itemList.Count}";

        for (var y = 0; y < _vm.ImageHeight; y += _vm.PixelLength)
            for (var x = 0; x < _vm.ImageWidth; x += _vm.PixelLength)
            {
                var tempColor = _vm.OriginImage[x, y];
                var min = 0;
                for (var k = 1; k < itemList.Count; ++k)
                    if (itemList[min].GetErr(tempColor) > itemList[k].GetErr(tempColor))
                        min = k;
                for (var xi = 0; xi < _vm.PixelLength; ++xi)
                    for (var yi = 0; yi < _vm.PixelLength; ++yi)
                        _vm.AfterImage[x + xi, y + yi] = itemList[min].Represent;
                ++itemList[min].Count;
            }

        await using var ms = new MemoryStream();
        await _vm.AfterImage.SaveAsync(ms, new PngEncoder());
        ms.Position = 0;
        await _vm.AfterBitmap.SetSourceAsync(ms.AsRandomAccessStream());
        AimChanged(_vm.LastPoint);
        _vm.IsLoaded = _vm.IsActed = true;
    }

    private void BToggleClick(object sender, TappedRoutedEventArgs e)
    {
        if (sender.To<AppBarToggleButton>().IsChecked is true)
            SetImageSource(_vm.AfterBitmap, ImageDisplaying.After, false);
        else
            SetImageSource(_vm.OriginBitmap, ImageDisplaying.Origin, false);
    }

    private async void BSaveClick(object sender, TappedRoutedEventArgs e)
    {
        if (_vm.OriginImage is null || _vm.AfterImage is null || _vm.SelectColorImage is null)
            return;
        if (await PickerHelper.PickSingleFolderAsync() is { } folder)
        {
            await (_vm.ImageDisplaying switch
            {
                ImageDisplaying.Origin => _vm.OriginImage.SaveAsync(folder.Path + "\\Origin.png"),
                ImageDisplaying.After => _vm.AfterImage.SaveAsync(folder.Path + "\\After.png"),
                ImageDisplaying.SelectColor => _vm.SelectColorImage.SaveAsync(folder.Path + $"\\SelectColor {_vm.LastSelectedColor.Represent.ToHex()}.png"),
                _ => ThrowHelper.ArgumentOutOfRange<ImageDisplaying, Task>(_vm.ImageDisplaying)
            });
        }
    }

    private async void ColorSelect(object sender, TappedRoutedEventArgs e)
    {
        if (((StackPanel)sender).Tag is ColorGroup colorGroup)
            await SelectColor(colorGroup);
    }

    private async void SubColorSelect(object sender, RightTappedRoutedEventArgs e)
    {
        if (((StackPanel)sender).Tag is ColorGroup colorGroup && colorGroup != _vm.LastSelectedColor)
            if (ColorGroup.RepresentEqual(_vm.LastSelectedSubColor, colorGroup))
                await SelectNewColor(_vm.LastSelectedColor, null);
            else
                await SelectNewColor(_vm.LastSelectedColor, colorGroup);
    }

    private async void IBitmapOnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        switch (_vm.ImageDisplaying)
        {
            case ImageDisplaying.After:
            {
                if (_vm.ItemList.Find(color => color.Set.Contains(_vm.CurrentCoordinate.Color)) is { } colorGroup)
                {
                    ListView.SelectedIndex = _vm.ItemList.IndexOf(colorGroup);
                    await SelectColor(colorGroup);
                }
                break;
            }
            case ImageDisplaying.SelectColor when _vm.AfterImage is null:
                return;
            case ImageDisplaying.SelectColor:
                SetImageSource(_vm.AfterBitmap, ImageDisplaying.After, true);
                break;
            case ImageDisplaying.Origin:
            default: break;
        }
    }

    private void CanvasOnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(Canvas);
        var (left, top) = _vm.BitmapPosition;
        if (currentPoint.Properties.IsLeftButtonPressed)
            _vm.BitmapPosition = (currentPoint.Position.X - _vm.LastPoint.X + left, currentPoint.Position.Y - _vm.LastPoint.Y + top);
        AimChanged(_vm.LastPoint = currentPoint.Position);
    }

    private void CanvasOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        _vm.BitmapScale += e.GetCurrentPoint(Canvas).Properties.MouseWheelDelta / 10000f;
        AimChanged(_vm.LastPoint);
    }

    private void CanvasOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RectSizeChanged();
        AimChanged(_vm.LastPoint);
        LHorizontal.X2 = Canvas.ActualWidth;
        LVertical.Y2 = Canvas.ActualHeight;
    }

    #endregion

    #region 操作

    private async Task SelectColor(ColorGroup color)
    {
        if (ColorGroup.RepresentEqual(_vm.LastSelectedColor, color))
        {
            if (_vm.SelectColorImage is null)
                return;
            if (_vm.ImageDisplaying is ImageDisplaying.SelectColor)
                SetImageSource(_vm.AfterBitmap, ImageDisplaying.After, true);
            else
                SetImageSource(_vm.SelectColorBitmap, ImageDisplaying.SelectColor, true);
        }
        else
            await SelectNewColor(color, _vm.LastSelectedSubColor);
    }

    private async Task SelectNewColor(ColorGroup newColor, ColorGroup? subColor)
    {
        if (_vm.AfterImage is null)
            return;
        _vm.LastSelectedColor = newColor;
        _vm.LastSelectedSubColor = subColor;

        _vm.SelectColorImage = new(_vm.AfterImage.Width, _vm.AfterImage.Height);
        for (var y = 0; y < _vm.AfterImage.Height; ++y)
            for (var x = 0; x < _vm.AfterImage.Width; ++x)
                _vm.SelectColorImage[x, y] = 0 switch
                {
                    0 when newColor.Set.Contains(_vm.AfterImage[x, y]) => Color.Red,
                    0 when subColor?.Set.Contains(_vm.AfterImage[x, y]) ?? false => Color.Black,
                    _ => Color.White
                };
        await using var ms = new MemoryStream();
        await _vm.SelectColorImage.SaveAsync(ms, new PngEncoder());
        ms.Position = 0;
        await _vm.SelectColorBitmap.SetSourceAsync(ms.AsRandomAccessStream());
        SetImageSource(_vm.SelectColorBitmap, ImageDisplaying.SelectColor, true);
    }

    private void SetImageSource(ImageSource source, ImageDisplaying imageDisplaying, bool keepProperty)
    {
        if (!keepProperty)
        {
            _vm.BitmapScale = 1;
            _vm.BitmapPosition = (0, 0);
        }
        IBitmap.Source = source;
        _vm.ImageDisplaying = imageDisplaying;
    }

    private void RectSizeChanged() => RgCanvas.Rect = new(0, 0, Canvas.ActualWidth, Canvas.ActualHeight);

    private void AimChanged(Point currentPoint)
    {
        if (_vm.OriginImage is null)
            return;

        var (left, top) = _vm.BitmapPosition;

        var xBitmap = currentPoint.X / _vm.BitmapScale - left;
        var yBitmap = currentPoint.Y / _vm.BitmapScale - top;
        var xPixel = Math.Min((int)((currentPoint.X - left) / (_vm.BitmapScale * _vm.PixelLength)) * _vm.PixelLength, _vm.ImageWidth - 1);
        var yPixel = Math.Min((int)((currentPoint.Y - top) / (_vm.BitmapScale * _vm.PixelLength)) * _vm.PixelLength, _vm.ImageHeight - 1);

        LVertical.X1 = LVertical.X2 = (xPixel + 0.5 * _vm.PixelLength) * _vm.BitmapScale + left;
        LHorizontal.Y1 = LHorizontal.Y2 = (yPixel + 0.5 * _vm.PixelLength) * _vm.BitmapScale + top;

        _vm.CurrentCoordinate = _vm.ImageDisplaying switch
        {
            ImageDisplaying.Origin when _vm.OriginImage is not null => (xBitmap, yBitmap, _vm.OriginImage[xPixel, yPixel]),
            ImageDisplaying.After when _vm.AfterImage is not null => (xBitmap, yBitmap, _vm.AfterImage[xPixel, yPixel]),
            ImageDisplaying.SelectColor when _vm.SelectColorImage is not null => (xBitmap, yBitmap, _vm.SelectColorImage[xPixel, yPixel]),
            _ => ThrowHelper.ArgumentOutOfRange<ImageDisplaying, (double, double, Rgba32)>(_vm.ImageDisplaying)
        };

        TbPointer.Text = $"Pointer:\nX:{xBitmap:F2}\nY:{yBitmap:F2}\nPixel:\nX:{xPixel / _vm.PixelLength}\nY:{yPixel / _vm.PixelLength}\n{_vm.CurrentCoordinate.Color.ToHex()}";
    }

    #endregion
}

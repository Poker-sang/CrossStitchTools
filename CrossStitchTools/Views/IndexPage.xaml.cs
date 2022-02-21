using CommunityToolkit.Mvvm.ComponentModel;
using CrossStitchTools.Interfaces;
using CrossStitchTools.Models;
using CrossStitchTools.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using Image = SixLabors.ImageSharp.Image;

namespace CrossStitchTools.Views;

[INotifyPropertyChanged]
public sealed partial class IndexPage : Page, ITypeGetter
{
    public IndexPage()
    {
        InitializeComponent();
    }

    public static Type TypeGetter => typeof(IndexPage);
    private bool _selectedColor;
    private Rgba32 _lastSelectedColor;

    private FileStream? _fileStream;
    private FileStream? FileStream
    {
        get => _fileStream;
        set
        {
            _fileStream = value;
            BSave.IsEnabled = BAction.IsEnabled = value is not null;
            BToggle.IsEnabled = false;
            BAction.Content = "Action";
        }
    }
    private Image<Rgba32>? _originImage;
    private Image<Rgba32>? _afterImage;
    private Image<Rgba32>? _nowImage;
    private readonly BitmapImage _originBitmapImage = new();
    private readonly BitmapImage _afterBitmapImage = new();
    private readonly BitmapImage _selectColorBitmapImage = new();

    [ObservableProperty] private List<ColorGroup> _itemList = new();

    private async void BActionClick(object sender, RoutedEventArgs e)
    {
        if (FileStream is null)
            return;
        BAction.IsEnabled = false;
        BAction.Content = "Acted";
        BToggle.IsEnabled = true;

        var colorErr = (int)(NbColor.Value is double.NaN ? 50 : NbColor.Value);
        var countErr = (int)(NbCount.Value is double.NaN ? 50 : NbCount.Value);
        var widthX = (int)(NbWidth.Value is double.NaN ? 50 : NbWidth.Value);
        var heightX = (int)(NbHeight.Value is double.NaN ? 50 : NbHeight.Value);

        var width = _originImage.Width / widthX;
        var height = _originImage.Height / heightX;
        _nowImage = _afterImage = new Image<Rgba32>(width, height);
        var dict = new Dictionary<Rgba32, int>();
        for (var y = 0; y < _originImage.Height; y += heightX)
            for (var x = 0; x < _originImage.Width; x += widthX)
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
        for (var y = 0; y < _originImage.Height; y += heightX)
            for (var x = 0; x < _originImage.Width; x += widthX)
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


        for (var y = 0; y < height; ++y)
            for (var x = 0; x < width; ++x)
            {
                var tempColor = _originImage[x * widthX, y * heightX];
                var min = 0;
                for (var k = 1; k < itemList.Count; k++)
                    if (itemList[min].GetErr(tempColor) > itemList[k].GetErr(tempColor))
                        min = k;
                _afterImage[x, y] = itemList[min].Represent;
                ++itemList[min].Count;
            }

        await using var ms = new MemoryStream();
        await _afterImage.SaveAsync(ms, new PngEncoder());
        ms.Position = 0;
        await _afterBitmapImage.SetSourceAsync(ms.AsRandomAccessStream());
    }
    private void BToggleClick(object sender, TappedRoutedEventArgs e)
    {
        IDisplay.Source = BToggle.IsChecked is true ? _afterBitmapImage : _originBitmapImage;
        _selectedColor = false;
    }

    private async void BOpen(object sender, RoutedEventArgs e)
    {
        if (await FileSystemHelper.GetStorageFile() is { } file)
        {
            if (FileStream is not null)
            {
                FileStream.Close();
                await FileStream.DisposeAsync();
            }
            FileStream = new FileStream(file.Path, FileMode.Open);
            await _originBitmapImage.SetSourceAsync(FileStream.AsRandomAccessStream());
            IDisplay.Source = _originBitmapImage;
            FileStream.Position = 0;
            _originImage = await Image.LoadAsync<Rgba32>(FileStream);
            TbPixel.Text = _originImage.Width + "x" + _originImage.Height;
        }
    }

    private async void BSaveClick(object sender, TappedRoutedEventArgs e)
    {
        if (_nowImage is not null)
            if (await FileSystemHelper.GetStorageFolder() is { } folder)
                await _nowImage.SaveAsync(folder.Path + "\\Save.png");
    }

    private void ColorTapped(object sender, TappedRoutedEventArgs e)
    {
        if (_lastSelectedColor == ((ColorGroup)((StackPanel)sender).Tag).Represent)
        {
            IDisplay.Source = _selectedColor ? _afterBitmapImage : _selectColorBitmapImage;
            _selectedColor = !_selectedColor;
        }
    }

    private async void SelectColorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_afterImage is null || e.AddedItems.Count is 0)
            return;
        var newColor = (ColorGroup)e.AddedItems[0];
        _lastSelectedColor = newColor.Represent;
        var selectColorImage = new Image<Rgba32>(_afterImage.Width, _afterImage.Height);
        for (var y = 0; y < _afterImage.Height; ++y)
            for (var x = 0; x < _afterImage.Width; ++x)
                selectColorImage[x, y] = newColor.Set.Contains(_afterImage[x, y])
                    ? Color.Black
                    : Color.White;
        await using var ms = new MemoryStream();
        await selectColorImage.SaveAsync(ms, new PngEncoder());
        ms.Position = 0;
        await _selectColorBitmapImage.SetSourceAsync(ms.AsRandomAccessStream());
        IDisplay.Source = _selectColorBitmapImage;
        _nowImage = selectColorImage;
        _selectedColor = true;
    }

}
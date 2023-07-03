using CommunityToolkit.Mvvm.ComponentModel;
using CrossStitchTools.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinUI3Utilities;

namespace CrossStitchTools;

[INotifyPropertyChanged]
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        CurrentContext.Window = this;
        InitializeComponent();
    }

    private void Loaded(object sender, RoutedEventArgs e)
    {
        NavigateFrame.Navigate<IndexPage>();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        DragZoneHelper.SetDragZones(new()
        {
#if DEBUG
            ExcludeDebugToolbarArea = true,
#endif
            DragZoneHeight = 32
        });
    }
}

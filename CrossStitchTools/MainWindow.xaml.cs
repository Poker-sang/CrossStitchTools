using CommunityToolkit.Mvvm.ComponentModel;
using CrossStitchTools.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace CrossStitchTools;

[INotifyPropertyChanged]
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        App.RootNavigationView = NavigationView;
        App.RootFrame = NavigateFrame;
        //TODO 标题栏
        //SetTitleBar(TitleBar);
    }
    private double PaneWidth => Math.Max(NavigationView.ActualWidth, NavigationView.CompactModeThresholdWidth) / 4;
    private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        NavigationView.PaneDisplayMode = NavigationView.ActualWidth < NavigationView.CompactModeThresholdWidth ? NavigationViewPaneDisplayMode.LeftCompact : NavigationViewPaneDisplayMode.Left;
        OnPropertyChanged(nameof(PaneWidth));
    }

    private void Loaded(object sender, RoutedEventArgs e)
    {
        _ = NavigateFrame.Navigate(typeof(IndexPage));
        NavigationView.SelectedItem = NavigationView.MenuItems[0];
        NavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left; //不加就不会显示PaneTitle
        OnPropertyChanged(nameof(PaneWidth));
    }

    private void BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs e)
    {
        NavigateFrame.GoBack();
        sender.SelectedItem = NavigateFrame.Content switch
        {
            IndexPage => sender.MenuItems[0],
            _ => sender.SelectedItem
        };
        sender.IsBackEnabled = NavigateFrame.CanGoBack;
    }

    private void ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is Type item && item != NavigateFrame.Content.GetType())
        {
            _ = NavigateFrame.Navigate(item);
            sender.IsBackEnabled = true;
            GC.Collect();
        }
    }
}
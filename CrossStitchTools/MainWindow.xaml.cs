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
        CurrentContext.TitleBar = TitleBar;
        CurrentContext.TitleTextBlock = TitleTextBlock;
        CurrentContext.NavigationView = NavigationView;
        CurrentContext.Frame = NavigateFrame;
    }

    private void Loaded(object sender, RoutedEventArgs e)
    {
        NavigationView.SettingsItem.To<NavigationViewItem>().Tag = typeof(SettingsPage);

       NavigationHelper.GotoPage<IndexPage>();
        NavigationView.SelectedItem = NavigationView.MenuItems[0];
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        DragZoneHelper.SetDragZones(new()
        {
#if DEBUG
            ExcludeDebugToolbarArea = true,
#endif
            DragZoneLeftIndent = (int)NavigationView.CompactPaneLength
        });
    }

    private void BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs e)
    {
        NavigateFrame.GoBack();
        NavigationView.SelectedItem = NavigateFrame.Content switch
        {
            IndexPage => NavigationView.MenuItems[0],
            SettingsPage => NavigationView.SettingsItem,
            _ => NavigationView.SelectedItem
        };
        NavigationView.IsBackEnabled = NavigateFrame.CanGoBack;
    }

    private void ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is Type item && item != NavigateFrame.Content.GetType())
            NavigationHelper.GotoPage(item);
    }
}

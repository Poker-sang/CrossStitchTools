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
        InitializeComponent();
        CurrentContext.TitleBar = TitleBar;
        CurrentContext.TitleTextBlock = TitleTextBlock;
        App.RootNavigationView = NavigationView;
        App.RootFrame = NavigateFrame;
    }

    private void Loaded(object sender, RoutedEventArgs e)
    {
       ((NavigationViewItem)NavigationView.SettingsItem).Tag = typeof(SettingsPage);

        _ = NavigateFrame.Navigate(typeof(IndexPage));
        NavigationView.SelectedItem = NavigationView.MenuItems[0];
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
        {
            _ = NavigateFrame.Navigate(item);
            NavigationView.IsBackEnabled = true;
            GC.Collect();
        }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using ModPC_Gui.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModPC_Gui
{
    public sealed partial class MainWindow : Window
    {
        private bool _isFirstLoad = true;
        private string _currentNavPosition = "Left";

        public MainWindow()
        {
            this.InitializeComponent();
            App.RegisterWindow(this);

            _currentNavPosition = App.GetNavigationPosition();
            SetNavigationPosition(_currentNavPosition, false);

            App.NavigationPositionChanged += OnNavigationPositionChanged;
            this.Closed += MainWindow_Closed;
            navigationView.ItemInvoked += NavigationView_ItemInvoked;

            // 设置初始页面
            contentFrame.Navigate(typeof(BlankPage1));
            navigationView.IsPaneOpen = false;

            // 设置初始导航项
            SetInitialNavigationSelection();
            BlankPage1.CloseNavigationPaneRequested += OnCloseNavigationPaneRequested;
        }

        private void OnCloseNavigationPaneRequested(object sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (navigationView != null)
                {
                    navigationView.IsPaneOpen = false;
                }
            });
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            App.NavigationPositionChanged -= OnNavigationPositionChanged;
            navigationView.ItemInvoked -= NavigationView_ItemInvoked;
            App.UnregisterWindow(this);
            BlankPage1.CloseNavigationPaneRequested -= OnCloseNavigationPaneRequested;
            this.Closed -= MainWindow_Closed;
        }

        private void OnNavigationPositionChanged(string position)
        {
            if (DispatcherQueue.HasThreadAccess)
            {
                SetNavigationPosition(position, true);
            }
            else
            {
                DispatcherQueue.TryEnqueue(() => SetNavigationPosition(position, true));
            }
        }

        private void SetNavigationPosition(string position, bool withAnimation)
        {
            if (position == _currentNavPosition && !_isFirstLoad) return;
            _currentNavPosition = position;

            if (navigationView != null)
            {
                navigationView.PaneDisplayMode = position == "Top"
                    ? NavigationViewPaneDisplayMode.Top
                    : NavigationViewPaneDisplayMode.Left;

                if (withAnimation)
                {
                    AnimateNavigationTransition();
                    navigationView.IsPaneOpen = false;
                }
            }

            _isFirstLoad = false;
        }

        private void AnimateNavigationTransition()
        {
            if (navigationView == null) return;

            var animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Storyboard.SetTarget(animation, navigationView);
            Storyboard.SetTargetProperty(animation, "Opacity");

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                contentFrame.Navigate(typeof(Pages.SettingsPage));
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();

                switch (navItemTag)
                {
                    case "home":
                        contentFrame.Navigate(typeof(Pages.BlankPage1));
                        break;
                    case "settings":
                        contentFrame.Navigate(typeof(Pages.SettingsPage));
                        break;
                }
            }
        }

        private void SetInitialNavigationSelection()
        {
            var homeItem = FindNavigationItemByTag("BlankPage1");
            if (homeItem != null)
            {
                DispatcherQueue.TryEnqueue(() => {
                    navigationView.SelectedItem = homeItem;
                });
            }
        }

        private NavigationViewItem FindNavigationItemByTag(string tag)
        {
            foreach (NavigationViewItem item in navigationView.MenuItems)
            {
                if (item.Tag is string itemTag && itemTag == tag)
                    return item;
            }

            foreach (NavigationViewItem item in navigationView.FooterMenuItems)
            {
                if (item.Tag is string itemTag && itemTag == tag)
                    return item;
            }

            return null;
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame.Navigate(typeof(SettingsPage));
                return;
            }

            var selectedItem = args.SelectedItem as NavigationViewItem;
            if (selectedItem?.Tag == null) return;

            string tag = selectedItem.Tag.ToString();
            Type pageType = tag switch
            {
                "BlankPage1" => typeof(BlankPage1),
                "BlankPage2" => typeof(BlankPage2),
                "BlankPage3" => typeof(BlankPage3),
                "BlankPage4" => typeof(BlankPage4),
                "BlankPage5" => typeof(BlankPage5),
                _ => null
            };

            if (pageType != null)
            {
                contentFrame.Navigate(pageType);
            }
        }
    }
}
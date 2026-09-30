using System;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using HelloWorldUWP.Core;
using HelloWorldUWP.Modules.CampusMap;
using HelloWorldUWP.Modules.Mensa;
using HelloWorldUWP.Modules.Card;
using HelloWorldUWP.Modules.News;
using HelloWorldUWP.Modules.Settings;
using HelloWorldUWP.Modules.Transport;

namespace HelloWorldUWP
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            RegisterAppModules();

            NavListView.ItemsSource = ModuleRegistry.Instance.Modules;

            // Handle Windows 10 Mobile hardware/virtual back button
            SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;

            // Toggle edge gesture strip visibility when pane opens/closes (14393 compatible)
            RootSplitView.RegisterPropertyChangedCallback(SplitView.IsPaneOpenProperty, (s, dp) =>
            {
                if (LeftEdgeGestureStrip != null)
                {
                    LeftEdgeGestureStrip.Visibility = RootSplitView.IsPaneOpen ? Visibility.Collapsed : Visibility.Visible;
                }
            });

            // Update active module title when language changes
            LocalizationService.Instance.LanguageChanged += (s, e) =>
            {
                if (NavListView.SelectedItem is IAppModule selectedModule)
                {
                    ModuleTitleText.Text = selectedModule.Title;
                }
            };

            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavListView.SelectedItem == null && ModuleRegistry.Instance.Modules.Count > 0)
            {
                NavListView.SelectedIndex = 0;
            }
        }

        private void RegisterAppModules()
        {
            var registry = ModuleRegistry.Instance;
            if (registry.Modules.Count == 0)
            {
                registry.RegisterModule(new MensaModule());
                registry.RegisterModule(new TransportModule());
                registry.RegisterModule(new CardModule());
                registry.RegisterModule(new NewsModule());
                registry.RegisterModule(new CampusMapModule());
                registry.RegisterModule(new SettingsModule());
            }
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            RootSplitView.IsPaneOpen = !RootSplitView.IsPaneOpen;
        }

        private void NavListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavListView.SelectedItem is IAppModule selectedModule)
            {
                ModuleTitleText.Text = selectedModule.Title;

                if (ContentFrame.CurrentSourcePageType != selectedModule.TargetPageType)
                {
                    ContentFrame.Navigate(selectedModule.TargetPageType);
                }

                // Close drawer on mobile after selection
                RootSplitView.IsPaneOpen = false;
            }
        }

        private void MainPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (RootSplitView.IsPaneOpen)
            {
                RootSplitView.IsPaneOpen = false;
                e.Handled = true;
                return;
            }

            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
                e.Handled = true;
                return;
            }
        }

        #region Swipe Gesture Navigation

        private void EdgeGesture_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Swipe right from the left edge to open the drawer
            if (!RootSplitView.IsPaneOpen && e.Cumulative.Translation.X > 35 && Math.Abs(e.Cumulative.Translation.X) > Math.Abs(e.Cumulative.Translation.Y))
            {
                RootSplitView.IsPaneOpen = true;
                e.Complete();
                e.Handled = true;
            }
        }

        private void EdgeGesture_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // Quick flick / swipe right from the left edge to open the drawer
            if (!RootSplitView.IsPaneOpen && (e.Cumulative.Translation.X > 25 || e.Velocities.Linear.X > 0.2))
            {
                RootSplitView.IsPaneOpen = true;
                e.Handled = true;
            }
        }

        private void Pane_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Swipe left on the open drawer to close it
            if (RootSplitView.IsPaneOpen && e.Cumulative.Translation.X < -35 && Math.Abs(e.Cumulative.Translation.X) > Math.Abs(e.Cumulative.Translation.Y))
            {
                RootSplitView.IsPaneOpen = false;
                e.Complete();
                e.Handled = true;
            }
        }

        private void Pane_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // Quick flick / swipe left on the drawer to close it
            if (RootSplitView.IsPaneOpen && (e.Cumulative.Translation.X < -25 || e.Velocities.Linear.X < -0.2))
            {
                RootSplitView.IsPaneOpen = false;
                e.Handled = true;
            }
        }

        private void TopAppBar_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (Math.Abs(e.Cumulative.Translation.X) > Math.Abs(e.Cumulative.Translation.Y))
            {
                if (!RootSplitView.IsPaneOpen && e.Cumulative.Translation.X > 35)
                {
                    RootSplitView.IsPaneOpen = true;
                    e.Complete();
                    e.Handled = true;
                }
                else if (RootSplitView.IsPaneOpen && e.Cumulative.Translation.X < -35)
                {
                    RootSplitView.IsPaneOpen = false;
                    e.Complete();
                    e.Handled = true;
                }
            }
        }

        private void TopAppBar_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (!RootSplitView.IsPaneOpen && (e.Cumulative.Translation.X > 25 || e.Velocities.Linear.X > 0.2))
            {
                RootSplitView.IsPaneOpen = true;
                e.Handled = true;
            }
            else if (RootSplitView.IsPaneOpen && (e.Cumulative.Translation.X < -25 || e.Velocities.Linear.X < -0.2))
            {
                RootSplitView.IsPaneOpen = false;
                e.Handled = true;
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using HelloWorldUWP.Modules.Transport.Models;
using HelloWorldUWP.Modules.Transport.Services;

namespace HelloWorldUWP.Modules.Transport.Views
{
    public sealed partial class TransportPage : Page
    {
        private readonly ITransitService _transitService;
        private readonly ObservableCollection<BusConnection> _connections = new ObservableCollection<BusConnection>();
        private List<BusConnection> _rawConnections = new List<BusConnection>();

        private TransitStop _fromStop;
        private TransitStop _toStop;
        private DispatcherTimer _liveTimer;
        private DateTimeOffset _lastUpdated = DateTimeOffset.MinValue;
        private int _autoRefreshCounter = 0;
        private bool _isInitializing = true;

        public TransportPage()
        {
            this.InitializeComponent();

            _transitService = new TransitousTransitService();
            _fromStop = _transitService.GetDefaultFromStop();
            _toStop = _transitService.GetDefaultToStop();

            ConnectionsListView.ItemsSource = _connections;

            // Setup 1-second live countdown timer
            _liveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _liveTimer.Tick += LiveTimer_Tick;

            this.Loaded += TransportPage_Loaded;
            this.Unloaded += TransportPage_Unloaded;
        }

        private async void TransportPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _isInitializing = true;
                FromAutoSuggestBox.Text = _fromStop.DisplayName;
                ToAutoSuggestBox.Text = _toStop.DisplayName;
            }
            finally
            {
                _isInitializing = false;
            }

            _liveTimer.Start();
            await LoadConnectionsAsync();
        }

        private void TransportPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _liveTimer.Stop();
        }

        private void LiveTimer_Tick(object sender, object e)
        {
            var now = DateTimeOffset.Now;

            // Update real-time countdown and progress bar on all visible connections
            foreach (var conn in _connections)
            {
                conn.UpdateProgress(now);
            }

            // Auto-refresh from API every 60 seconds
            _autoRefreshCounter++;
            if (_autoRefreshCounter >= 60)
            {
                _autoRefreshCounter = 0;
                var task = LoadConnectionsAsync(silent: true);
            }
        }

        private async Task LoadConnectionsAsync(bool silent = false)
        {
            if (_fromStop == null || _toStop == null) return;

            if (!silent)
            {
                SetViewState(loading: true);
            }

            try
            {
                var connections = await _transitService.GetBusConnectionsAsync(_fromStop.Id, _toStop.Id);
                _rawConnections = connections;
                _lastUpdated = DateTimeOffset.Now;
                LastUpdatedText.Text = $"Updated {_lastUpdated.ToString("HH:mm:ss")}";

                ApplyFilters();

                if (_connections.Count == 0)
                {
                    SetViewState(empty: true);
                }
                else
                {
                    SetViewState(content: true);
                }
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    ErrorMessageText.Text = $"Could not load bus connections: {ex.Message}";
                    SetViewState(error: true);
                }
            }
        }

        private void ApplyFilters()
        {
            bool directOnly = DirectOnlyCheckBox.IsChecked == true;

            var filtered = directOnly
                ? _rawConnections.Where(c => c.IsDirect).ToList()
                : _rawConnections;

            var now = DateTimeOffset.Now;
            _connections.Clear();
            foreach (var item in filtered)
            {
                item.UpdateProgress(now);
                _connections.Add(item);
            }
        }

        private void SetViewState(bool loading = false, bool error = false, bool empty = false, bool content = false)
        {
            LoadingPanel.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
            ErrorPanel.Visibility = error ? Visibility.Visible : Visibility.Collapsed;
            EmptyPanel.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            ConnectionsListView.Visibility = content ? Visibility.Visible : Visibility.Collapsed;
        }

        #region Swap & Controls

        private async void SwapButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformSwapAsync();
        }

        private async void SwapAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformSwapAsync();
        }

        private async Task PerformSwapAsync()
        {
            var temp = _fromStop;
            _fromStop = _toStop;
            _toStop = temp;

            try
            {
                _isInitializing = true;
                FromAutoSuggestBox.Text = _fromStop.DisplayName;
                ToAutoSuggestBox.Text = _toStop.DisplayName;
            }
            finally
            {
                _isInitializing = false;
            }

            await LoadConnectionsAsync();
        }

        private void DirectOnlyCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
            if (_connections.Count == 0)
            {
                SetViewState(empty: true);
            }
            else
            {
                SetViewState(content: true);
            }
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadConnectionsAsync();
        }

        private async void RefreshAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadConnectionsAsync();
        }

        private async void TryAgainButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadConnectionsAsync();
        }

        private async void AboutAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog(
                "Public transit routing and timetable data are provided by Transitous, an open-source community routing engine powered by MOTIS 2 using open GTFS data from DELFI (Germany).\n\nLine coverage includes Saarbahn buses and regional connections in Saarland.",
                "About Open Transit Data");
            await dialog.ShowAsync();
        }

        #endregion

        #region Autocomplete Handlers

        private async void FromAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (_isInitializing || args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

            var suggestions = await _transitService.GetStopSuggestionsAsync(sender.Text);
            sender.ItemsSource = suggestions;
        }

        private async void FromAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is TransitStop stop)
            {
                _fromStop = stop;
                sender.Text = stop.DisplayName;
                await LoadConnectionsAsync();
            }
        }

        private async void FromAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is TransitStop stop)
            {
                _fromStop = stop;
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                var resolved = await _transitService.ResolveStopAsync(args.QueryText);
                if (resolved != null)
                {
                    _fromStop = resolved;
                    sender.Text = resolved.DisplayName;
                }
            }
            await LoadConnectionsAsync();
        }

        private async void ToAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (_isInitializing || args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

            var suggestions = await _transitService.GetStopSuggestionsAsync(sender.Text);
            sender.ItemsSource = suggestions;
        }

        private async void ToAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is TransitStop stop)
            {
                _toStop = stop;
                sender.Text = stop.DisplayName;
                await LoadConnectionsAsync();
            }
        }

        private async void ToAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is TransitStop stop)
            {
                _toStop = stop;
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                var resolved = await _transitService.ResolveStopAsync(args.QueryText);
                if (resolved != null)
                {
                    _toStop = resolved;
                    sender.Text = resolved.DisplayName;
                }
            }
            await LoadConnectionsAsync();
        }

        #endregion
    }
}

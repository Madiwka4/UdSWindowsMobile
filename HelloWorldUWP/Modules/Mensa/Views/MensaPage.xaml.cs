using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using HelloWorldUWP.Modules.Mensa.Models;
using HelloWorldUWP.Modules.Mensa.Services;
using HelloWorldUWP.Services;

namespace HelloWorldUWP.Modules.Mensa.Views
{
    public sealed partial class MensaPage : Page
    {
        private readonly IOpenMensaService _mensaService;
        private List<Canteen> _canteens;
        private List<CanteenDay> _days;
        private bool _isInitializing = true;

        public MensaPage()
        {
            this.InitializeComponent();
            _mensaService = new OpenMensaService();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            _isInitializing = true;
            try
            {
                SetViewState(Loading: true);

                _canteens = await _mensaService.GetCanteensAsync();
                CanteenComboBox.ItemsSource = _canteens;
                CanteenComboBox.SelectedIndex = 0; // Default to Mensa Saarbrücken (261)

                var selectedCanteen = _canteens[0];
                await LoadDaysForCanteenAsync(selectedCanteen.Id);
            }
            finally
            {
                _isInitializing = false;
            }

            await LoadMealsAsync(forceRefresh: false);
        }

        private async Task LoadDaysForCanteenAsync(int canteenId)
        {
            _days = await _mensaService.GetOpenDaysAsync(canteenId);
            DayComboBox.ItemsSource = _days;

            if (_days != null && _days.Count > 0)
            {
                // Select today if open, otherwise first available open day
                var today = DateTime.Today;
                var todayDay = _days.FirstOrDefault(d => d.Date.Date == today && !d.IsClosed);
                if (todayDay != null)
                {
                    DayComboBox.SelectedItem = todayDay;
                }
                else
                {
                    DayComboBox.SelectedIndex = 0;
                }
            }
        }

        private async Task LoadMealsAsync(bool forceRefresh)
        {
            if (CanteenComboBox.SelectedItem == null || DayComboBox.SelectedItem == null)
            {
                return;
            }

            var selectedCanteen = (Canteen)CanteenComboBox.SelectedItem;
            var selectedDay = (CanteenDay)DayComboBox.SelectedItem;

            SetViewState(Loading: true);

            var result = await _mensaService.GetMealsAsync(selectedCanteen.Id, selectedDay.Date, forceRefresh);

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage) && (result.Meals == null || result.Meals.Count == 0))
            {
                ErrorMessageText.Text = result.ErrorMessage;
                SetViewState(Error: true);
                return;
            }

            if (result.IsClosed || result.Meals == null || result.Meals.Count == 0)
            {
                ClosedStatusTitle.Text = $"{selectedCanteen.ShortName} geschlossen";
                ClosedStatusMessage.Text = $"Für {selectedDay.DisplayText} sind keine Speiseplandaten vorhanden.";
                SetViewState(Closed: true);

                if (selectedDay.Date.Date == DateTime.Today)
                {
                    LiveTileService.UpdateLiveTile(selectedCanteen.ShortName, null, selectedDay.Date);
                }
                return;
            }

            // Successfully loaded meals
            MealsListView.ItemsSource = result.Meals;
            OfflineBanner.Visibility = result.IsFromCache ? Visibility.Visible : Visibility.Collapsed;
            SetViewState(Content: true);

            // Update Start Screen Live Tile with today's meals
            if (selectedDay.Date.Date == DateTime.Today)
            {
                LiveTileService.UpdateLiveTile(selectedCanteen.ShortName, result.Meals, selectedDay.Date);
            }
        }

        private async void CanteenComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || CanteenComboBox.SelectedItem == null) return;

            var selectedCanteen = (Canteen)CanteenComboBox.SelectedItem;
            await LoadDaysForCanteenAsync(selectedCanteen.Id);
            await LoadMealsAsync(forceRefresh: false);
        }

        private async void DayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || DayComboBox.SelectedItem == null) return;

            await LoadMealsAsync(forceRefresh: false);
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadMealsAsync(forceRefresh: true);
        }

        private void NextOpenDayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_days != null && _days.Count > 0)
            {
                int nextIndex = DayComboBox.SelectedIndex + 1;
                if (nextIndex < _days.Count)
                {
                    DayComboBox.SelectedIndex = nextIndex;
                }
                else
                {
                    DayComboBox.SelectedIndex = 0;
                }
            }
        }

        private void SetViewState(bool Loading = false, bool Closed = false, bool Error = false, bool Content = false)
        {
            LoadingPanel.Visibility = Loading ? Visibility.Visible : Visibility.Collapsed;
            ClosedPanel.Visibility = Closed ? Visibility.Visible : Visibility.Collapsed;
            ErrorPanel.Visibility = Error ? Visibility.Visible : Visibility.Collapsed;
            MealsListView.Visibility = Content ? Visibility.Visible : Visibility.Collapsed;

            if (Loading || Closed || Error)
            {
                OfflineBanner.Visibility = Visibility.Collapsed;
            }
        }
    }
}

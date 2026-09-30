using System;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using HelloWorldUWP.Core;
using HelloWorldUWP.Modules.Card.Models;
using HelloWorldUWP.Modules.Card.Services;

namespace HelloWorldUWP.Modules.Card.Views
{
    public sealed partial class CardPage : Page
    {
        private readonly INfcCardReaderService _nfcService;
        private readonly LocalizationService _loc = LocalizationService.Instance;
        private MensaCardData _currentCard;

        public CardPage()
        {
            this.InitializeComponent();

            _nfcService = new NfcCardReaderService();

            this.Loaded += CardPage_Loaded;
            this.Unloaded += CardPage_Unloaded;
        }

        private async void CardPage_Loaded(object sender, RoutedEventArgs e)
        {
            _loc.LanguageChanged += Loc_LanguageChanged;
            _nfcService.CardDetected += NfcService_CardDetected;
            _nfcService.StatusChanged += NfcService_StatusChanged;
            _nfcService.ErrorOccurred += NfcService_ErrorOccurred;

            UpdateLocalizedStrings();

            // Load last cached balance if available
            var cached = _nfcService.GetLastCachedCard();
            if (cached != null)
            {
                DisplayCard(cached);
                LastReadTimeText.Text = $"{_loc["Card_Cached_Prefix"]}{cached.FormattedReadTime}";
            }
            else
            {
                LastReadTimeText.Text = _loc["Card_Tap_Prompt"];
            }

            StatusMessageText.Text = _loc["Card_Status_Active"];

            await _nfcService.StartListeningAsync();
        }

        private void CardPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _loc.LanguageChanged -= Loc_LanguageChanged;
            _nfcService.CardDetected -= NfcService_CardDetected;
            _nfcService.StatusChanged -= NfcService_StatusChanged;
            _nfcService.ErrorOccurred -= NfcService_ErrorOccurred;

            _nfcService.StopListening();
        }

        private void Loc_LanguageChanged(object sender, EventArgs e)
        {
            UpdateLocalizedStrings();
            if (_currentCard != null)
            {
                DisplayCard(_currentCard);
            }
            else
            {
                LastReadTimeText.Text = _loc["Card_Tap_Prompt"];
                StatusMessageText.Text = _loc["Card_Status_Active"];
            }
        }

        private void UpdateLocalizedStrings()
        {
            CardHeaderTitleText.Text = _loc["Card_Header_Title"];
            CardHeaderSubtitleText.Text = _loc["Card_Header_Subtitle"];
            CurrentBalanceLabelText.Text = _loc["Card_Balance_Label"];
            InstitutionFooterText.Text = _loc["Card_Institution"];
            NfcSectionTitleText.Text = _loc["Card_Nfc_Section_Title"];
            NfcStatusDescriptionText.Text = _loc["Card_Nfc_Section_Desc"];
            ClearButtonText.Text = _loc["Card_Clear_Button"];
        }

        private async void NfcService_CardDetected(object sender, MensaCardData card)
        {
            _currentCard = card;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DisplayCard(card);
            });
        }

        private async void NfcService_StatusChanged(object sender, string status)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusMessageText.Text = status;
                StatusDot.Fill = new SolidColorBrush(Color.FromArgb(255, 16, 124, 65)); // Green
            });
        }

        private async void NfcService_ErrorOccurred(object sender, string error)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusMessageText.Text = error;
                StatusDot.Fill = new SolidColorBrush(Color.FromArgb(255, 232, 17, 35)); // Red
            });
        }

        private void DisplayCard(MensaCardData card)
        {
            _currentCard = card;
            BalanceAmountText.Text = card.FormattedBalance;
            LastReadTimeText.Text = card.IsSimulated
                ? $"{_loc["Card_Simulated_Prefix"]}{card.FormattedReadTime}"
                : $"{_loc["Card_Read_Prefix"]}{card.FormattedReadTime}";
            CardTypeFooterText.Text = $"{card.CardType}";
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                settings.Remove("CachedCard_Balance");
                settings.Remove("CachedCard_Time");
                settings.Remove("CachedCard_Type");
            }
            catch { }

            _currentCard = null;
            BalanceAmountText.Text = "--,-- €";
            LastReadTimeText.Text = _loc["Card_Tap_Prompt"];
            CardTypeFooterText.Text = "MIFARE DESFire EV1 (InterCard)";
            StatusMessageText.Text = _loc["Card_Status_Cleared"];
            StatusDot.Fill = new SolidColorBrush(Color.FromArgb(255, 16, 124, 65));
        }
    }
}

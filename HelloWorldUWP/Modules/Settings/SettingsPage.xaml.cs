using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using HelloWorldUWP.Core;

namespace HelloWorldUWP.Modules.Settings
{
    public sealed partial class SettingsPage : Page
    {
        private readonly LocalizationService _loc = LocalizationService.Instance;

        public SettingsPage()
        {
            this.InitializeComponent();

            this.Loaded += SettingsPage_Loaded;
            this.Unloaded += SettingsPage_Unloaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _loc.LanguageChanged += Loc_LanguageChanged;

            // Set radio button selection to current language
            if (_loc.IsGerman)
            {
                RadioGerman.IsChecked = true;
            }
            else
            {
                RadioEnglish.IsChecked = true;
            }

            UpdateLocalizedStrings();
        }

        private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _loc.LanguageChanged -= Loc_LanguageChanged;
        }

        private void Loc_LanguageChanged(object sender, EventArgs e)
        {
            UpdateLocalizedStrings();
        }

        private void RadioGerman_Click(object sender, RoutedEventArgs e)
        {
            _loc.SetLanguage(LocalizationService.LangGerman);
        }

        private void RadioEnglish_Click(object sender, RoutedEventArgs e)
        {
            _loc.SetLanguage(LocalizationService.LangEnglish);
        }

        private void UpdateLocalizedStrings()
        {
            SettingsTitleText.Text = _loc["Settings_Title"];
            LanguageHeaderText.Text = _loc["Settings_Lang_Header"];
            LanguageDescText.Text = _loc["Settings_Lang_Desc"];
            RadioGerman.Content = _loc["Settings_Lang_German"];
            RadioEnglish.Content = _loc["Settings_Lang_English"];

            AppInfoHeaderText.Text = _loc["Settings_AppInfo_Header"];
            AppInfoVersionText.Text = _loc["Settings_AppInfo_Version"];
            AppInfoDescText.Text = _loc["Settings_AppInfo_Desc"];

            DataSourcesHeaderText.Text = _loc["Settings_Data_Header"];
            DataSourcesDescText.Text = _loc["Settings_Data_Desc"];

            CacheHeaderText.Text = _loc["Settings_Cache_Header"];
            CacheDescText.Text = _loc["Settings_Cache_Desc"];
            ClearCacheButton.Content = _loc["Settings_Cache_Button"];
        }

        private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var files = await folder.GetFilesAsync();
                int count = 0;
                foreach (var file in files)
                {
                    if (file.Name.StartsWith("meals_") || file.Name.StartsWith("days_") || file.Name.StartsWith("transitous_"))
                    {
                        await file.DeleteAsync();
                        count++;
                    }
                }
                CacheStatusText.Text = _loc.GetString("Settings_Cache_Cleared", count);
            }
            catch (Exception ex)
            {
                CacheStatusText.Text = _loc.GetString("Settings_Cache_Error", ex.Message);
            }
        }
    }
}

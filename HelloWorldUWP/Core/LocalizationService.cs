using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Globalization;
using Windows.Storage;

namespace HelloWorldUWP.Core
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService _instance;
        public static LocalizationService Instance => _instance ?? (_instance = new LocalizationService());

        public const string LangGerman = "de";
        public const string LangEnglish = "en";

        private const string CacheKeyLanguage = "AppLanguage";
        private string _currentLanguage = LangGerman;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler LanguageChanged;

        public string CurrentLanguage
        {
            get => _currentLanguage;
            private set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsGerman));
                    OnPropertyChanged(nameof(IsEnglish));
                    OnPropertyChanged("Item[]");
                }
            }
        }

        public bool IsGerman => CurrentLanguage == LangGerman;
        public bool IsEnglish => CurrentLanguage == LangEnglish;

        public string this[string key] => GetString(key);

        private readonly Dictionary<string, Dictionary<string, string>> _strings = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public LocalizationService()
        {
            InitializeDictionaries();
        }

        public void Initialize()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                if (settings.ContainsKey(CacheKeyLanguage) && settings[CacheKeyLanguage] is string savedLang)
                {
                    if (savedLang == LangEnglish || savedLang == LangGerman)
                    {
                        ApplyLanguage(savedLang, saveToSettings: false);
                        return;
                    }
                }

                // If not explicitly saved, detect system language
                string sysLang = ApplicationLanguages.Languages.Count > 0 ? ApplicationLanguages.Languages[0] : "de";
                string detected = sysLang.StartsWith("de", StringComparison.OrdinalIgnoreCase) ? LangGerman : LangEnglish;
                ApplyLanguage(detected, saveToSettings: false);
            }
            catch
            {
                ApplyLanguage(LangGerman, saveToSettings: false);
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (languageCode != LangGerman && languageCode != LangEnglish)
            {
                languageCode = LangGerman;
            }

            ApplyLanguage(languageCode, saveToSettings: true);
        }

        private void ApplyLanguage(string languageCode, bool saveToSettings)
        {
            CurrentLanguage = languageCode;

            try
            {
                ApplicationLanguages.PrimaryLanguageOverride = languageCode == LangGerman ? "de-DE" : "en-US";
            }
            catch { }

            if (saveToSettings)
            {
                try
                {
                    ApplicationData.Current.LocalSettings.Values[CacheKeyLanguage] = languageCode;
                }
                catch { }
            }

            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetString(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            string text = null;
            if (_strings.TryGetValue(CurrentLanguage, out var langDict))
            {
                langDict.TryGetValue(key, out text);
            }

            if (text == null && _strings.TryGetValue(LangEnglish, out var fallbackDict))
            {
                fallbackDict.TryGetValue(key, out text);
            }

            if (text == null && _strings.TryGetValue(LangGerman, out var deDict))
            {
                deDict.TryGetValue(key, out text);
            }

            if (text == null)
            {
                return key;
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(text, args);
                }
                catch
                {
                    return text;
                }
            }

            return text;
        }

        private void InitializeDictionaries()
        {
            var de = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            #region German Strings
            // Navigation
            de["Nav_Mensa"] = "Mensa";
            de["Nav_Mensa_Desc"] = "Speisepläne der UdS & Saarland Canteens";
            de["Nav_Transport"] = "Bus & Bahn";
            de["Nav_Transport_Desc"] = "Live-Fahrpläne Saarbrücken & Campus";
            de["Nav_Card"] = "UdS-Card Guthaben";
            de["Nav_Card_Desc"] = "Offline-NFC-Kartenleser für Mensakarte";
            de["Nav_CampusMap"] = "Campus-Karte";
            de["Nav_CampusMap_Desc"] = "Lagepläne und Gebäude-Navigation";
            de["Nav_News"] = "Campus-News";
            de["Nav_News_Desc"] = "Aktuelle Meldungen der Universität";
            de["Nav_Settings"] = "Einstellungen";
            de["Nav_Settings_Desc"] = "Sprache, Cache & Info";

            // Settings Page
            de["Settings_Title"] = "Einstellungen & Info";
            de["Settings_Lang_Header"] = "Sprache / Language";
            de["Settings_Lang_Desc"] = "Wählen Sie die Anzeigesprache für die Saarland Uni App.";
            de["Settings_Lang_German"] = "Deutsch (German)";
            de["Settings_Lang_English"] = "English (Englisch)";
            de["Settings_AppInfo_Header"] = "Saarland Uni";
            de["Settings_AppInfo_Version"] = "Version 1.0 (Windows 10 Mobile Edition)";
            de["Settings_AppInfo_Desc"] = "Inoffizielle App für Studierende und Beschäftigte der Universität des Saarlandes (UdS). Entwickelt und optimiert für Windows 10 Mobile Geräte.";
            de["Settings_Data_Header"] = "Datenquellen";
            de["Settings_Data_Desc"] = "Mensa-Speisepläne werden über die offizielle OpenMensa API v2 (openmensa.org) und das Studierendenwerk Saarland (mensaar.de) bezogen. Fahrplandaten stammen vom europäischen Transitous-Routingnetzwerk (MOTIS 2).";
            de["Settings_Cache_Header"] = "Offline-Speicher";
            de["Settings_Cache_Desc"] = "Speisepläne und Kartendaten werden lokal auf dem Telefon zwischengespeichert, damit Sie sie auch ohne Mobilfunkempfang im Hörsaal einsehen können.";
            de["Settings_Cache_Button"] = "Cache jetzt leeren";
            de["Settings_Cache_Cleared"] = "Offline-Cache geleert ({0} Dateien entfernt).";
            de["Settings_Cache_Error"] = "Fehler beim Leeren des Caches: {0}";

            // UdS-Card Page
            de["Card_Header_Title"] = "Mensa-Kartenguthaben";
            de["Card_Header_Subtitle"] = "Offline-NFC-Lesegerät • Universität des Saarlandes";
            de["Card_Balance_Label"] = "AKTUELLES GUTHABEN";
            de["Card_Tap_Prompt"] = "Karte anhalten zum Lesen";
            de["Card_Read_Prefix"] = "Gelesen: ";
            de["Card_Cached_Prefix"] = "Gespeichert: ";
            de["Card_Simulated_Prefix"] = "Simuliert: ";
            de["Card_Nfc_Section_Title"] = "Offline-NFC-Kartenleser";
            de["Card_Nfc_Section_Desc"] = "Halten Sie Ihre UdS-Card an die Rückseite Ihres Telefons.";
            de["Card_Status_Active"] = "NFC-Kartenleser ist aktiv und wartet...";
            de["Card_Status_Detected"] = "Karte erkannt! Guthaben wird gelesen...";
            de["Card_Status_Success"] = "Karte erfolgreich gelesen ({0})";
            de["Card_Status_Error"] = "Fehler beim Lesen der Karte";
            de["Card_Status_NoDecode"] = "Karte erkannt, aber kein Guthaben gefunden";
            de["Card_Status_InitError"] = "NFC-Initialisierungsfehler";
            de["Card_Status_Paused"] = "Kartenleser pausiert";
            de["Card_Status_Cleared"] = "Karte geleert • Bereit zum Scannen";
            de["Card_Clear_Button"] = "Karte leeren";
            de["Card_Institution"] = "Studierendenwerk Saarland";

            // Common / Shared
            de["Common_Reload"] = "Aktualisieren";
            de["Common_Close"] = "Schließen";
            de["Common_Search"] = "Suchen...";
            #endregion

            #region English Strings
            // Navigation
            en["Nav_Mensa"] = "Canteens";
            en["Nav_Mensa_Desc"] = "Meal plans for UdS & Saarland canteens";
            en["Nav_Transport"] = "Transit & Bus";
            en["Nav_Transport_Desc"] = "Live bus departures for Saarbrücken & Campus";
            en["Nav_Card"] = "Mensa Card Balance";
            en["Nav_Card_Desc"] = "Offline NFC reader for UdS-Card";
            en["Nav_CampusMap"] = "Campus Map";
            en["Nav_CampusMap_Desc"] = "Campus layouts and building navigation";
            en["Nav_News"] = "Campus News";
            en["Nav_News_Desc"] = "Latest updates from Saarland University";
            en["Nav_Settings"] = "Settings";
            en["Nav_Settings_Desc"] = "Language, Cache & Info";

            // Settings Page
            en["Settings_Title"] = "Settings & Info";
            en["Settings_Lang_Header"] = "Language / Sprache";
            en["Settings_Lang_Desc"] = "Select your preferred display language for the Saarland Uni app.";
            en["Settings_Lang_German"] = "Deutsch (German)";
            en["Settings_Lang_English"] = "English";
            en["Settings_AppInfo_Header"] = "Saarland Uni";
            en["Settings_AppInfo_Version"] = "Version 1.0 (Windows 10 Mobile Edition)";
            en["Settings_AppInfo_Desc"] = "Unofficial app for students and staff of Saarland University (UdS). Designed and optimized for Windows 10 Mobile devices.";
            en["Settings_Data_Header"] = "Data Sources";
            en["Settings_Data_Desc"] = "Canteen meal plans are sourced from the official OpenMensa API v2 (openmensa.org) and Studierendenwerk Saarland (mensaar.de). Bus departure data is provided by the European Transitous routing network (MOTIS 2).";
            en["Settings_Cache_Header"] = "Offline Storage";
            en["Settings_Cache_Desc"] = "Meal plans and transit information are cached locally on your device so you can access them even without cellular reception in lecture halls.";
            en["Settings_Cache_Button"] = "Clear Cache Now";
            en["Settings_Cache_Cleared"] = "Offline cache cleared ({0} files removed).";
            en["Settings_Cache_Error"] = "Error clearing cache: {0}";

            // UdS-Card Page
            en["Card_Header_Title"] = "Mensa Card Balance";
            en["Card_Header_Subtitle"] = "Offline NFC Reader • Universität des Saarlandes";
            en["Card_Balance_Label"] = "CURRENT BALANCE";
            en["Card_Tap_Prompt"] = "Tap card to read";
            en["Card_Read_Prefix"] = "Read: ";
            en["Card_Cached_Prefix"] = "Cached: ";
            en["Card_Simulated_Prefix"] = "Simulated: ";
            en["Card_Nfc_Section_Title"] = "Offline NFC Reader";
            en["Card_Nfc_Section_Desc"] = "Hold your UdS-Card against the back of your phone.";
            en["Card_Status_Active"] = "NFC Reader is active and listening...";
            en["Card_Status_Detected"] = "Card detected! Reading balance...";
            en["Card_Status_Success"] = "Card read successfully ({0})";
            en["Card_Status_Error"] = "Card reading error";
            en["Card_Status_NoDecode"] = "Card read finished (no balance decoded)";
            en["Card_Status_InitError"] = "NFC initialization error";
            en["Card_Status_Paused"] = "Reader paused";
            en["Card_Status_Cleared"] = "Card cleared • Ready to scan";
            en["Card_Clear_Button"] = "Clear Card";
            en["Card_Institution"] = "Studierendenwerk Saarland";

            // Common / Shared
            en["Common_Reload"] = "Reload";
            en["Common_Close"] = "Close";
            en["Common_Search"] = "Search...";
            #endregion

            _strings[LangGerman] = de;
            _strings[LangEnglish] = en;
        }

        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}

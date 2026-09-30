using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace HelloWorldUWP.Modules.Mensa.Converters
{
    /// <summary>
    /// Maps category names to their respective background color:
    /// - Menu 1: Red (#E81123)
    /// - Menu 2: Blue (#0078D7)
    /// - Wahlessen: Yellow (#FFB900)
    /// - Mensacafé: Green (#107C41)
    /// </summary>
    public class CategoryToBackgroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromArgb(255, 232, 17, 35));
        private static readonly SolidColorBrush BlueBrush = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
        private static readonly SolidColorBrush YellowBrush = new SolidColorBrush(Color.FromArgb(255, 255, 185, 0));
        private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromArgb(255, 16, 124, 65));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var cat = (value as string ?? "").Trim().ToLowerInvariant();
            if (cat.Contains("1")) return RedBrush;
            if (cat.Contains("2")) return BlueBrush;
            if (cat.Contains("wahl")) return YellowBrush;
            if (cat.Contains("caf") || cat.Contains("mensacaf")) return GreenBrush;
            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Maps category names to appropriate text foreground for high contrast:
    /// - Yellow background gets dark text (#1A1A1A)
    /// - Red, Blue, Green get white text (#FFFFFF)
    /// </summary>
    public class CategoryToForegroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush WhiteBrush = new SolidColorBrush(Colors.White);
        private static readonly SolidColorBrush DarkBrush = new SolidColorBrush(Color.FromArgb(255, 26, 26, 26));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var cat = (value as string ?? "").Trim().ToLowerInvariant();
            if (cat.Contains("wahl")) return DarkBrush;
            return WhiteBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Maps category names to matching text accent colors for location/counter labels.
    /// </summary>
    public class CategoryToAccentConverter : IValueConverter
    {
        private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromArgb(255, 232, 17, 35));
        private static readonly SolidColorBrush BlueBrush = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
        private static readonly SolidColorBrush YellowBrush = new SolidColorBrush(Color.FromArgb(255, 255, 185, 0));
        private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromArgb(255, 16, 124, 65));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var cat = (value as string ?? "").Trim().ToLowerInvariant();
            if (cat.Contains("1")) return RedBrush;
            if (cat.Contains("2")) return BlueBrush;
            if (cat.Contains("wahl")) return YellowBrush;
            if (cat.Contains("caf") || cat.Contains("mensacaf")) return GreenBrush;
            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

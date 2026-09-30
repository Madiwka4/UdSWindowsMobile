using System;
using System.Globalization;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace HelloWorldUWP.Modules.Transport.Converters
{
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    hex = hex.Trim().TrimStart('#');
                    if (hex.Length == 6)
                    {
                        byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                        byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                        byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                        return new SolidColorBrush(Color.FromArgb(255, r, g, b));
                    }
                    if (hex.Length == 8)
                    {
                        byte a = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                        byte r = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                        byte g = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                        byte b = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                        return new SolidColorBrush(Color.FromArgb(a, r, g, b));
                    }
                }
                catch
                {
                    // Fall back to parameter or default
                }
            }

            if (parameter is string defaultColor && defaultColor == "Accent")
            {
                return new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = parameter is string p && p == "Invert";
            bool isEmpty = value == null || (value is string s && string.IsNullOrWhiteSpace(s));

            if (invert)
            {
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            }
            return isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

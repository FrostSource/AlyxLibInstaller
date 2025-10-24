using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AlyxLibInstallerWPF
{
    // Converter for null checking in XAML
    public class IsNotNullConverter : IValueConverter
    {
        public static IsNotNullConverter Instance { get; } = new IsNotNullConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for comparing numbers
    public class GreaterThanConverter : IValueConverter
    {
        public static GreaterThanConverter Instance { get; } = new GreaterThanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter is string paramStr && int.TryParse(paramStr, out int threshold))
            {
                return intValue > threshold;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AlyxLibInstallerWPF.Converters;
/// <summary>
/// Shows an element when the string is empty, e.g. a placeholder.
/// </summary>
public class InvertStringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = value as string;
        return string.IsNullOrWhiteSpace(str) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
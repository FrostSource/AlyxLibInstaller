using System.Globalization;
using System.Windows.Data;

namespace AlyxLibInstallerWPF.Converters;
public class EscapeUnderscoreConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        return value.ToString()?.Replace("_", "__") ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

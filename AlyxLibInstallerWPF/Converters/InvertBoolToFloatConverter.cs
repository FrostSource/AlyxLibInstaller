using System.Globalization;
using System.Windows.Data;

namespace AlyxLibInstallerWPF.Converters;
public class InvertBoolToFloatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((bool)value) ? 0.0f : 1.0f;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (float)value <= 0.0f;
    }
}

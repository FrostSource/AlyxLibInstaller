using System.Globalization;
using System.Windows.Data;

namespace AlyxLibInstallerWPF.Converters;
public class BoolToBoolNullableConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool?)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return false;
        return (bool)value;
    }
}

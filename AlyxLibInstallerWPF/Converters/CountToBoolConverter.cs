using System.Globalization;
using System.Windows.Data;

namespace AlyxLibInstallerWPF.Converters;
public class CountToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count && count > 0) return true;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (value is bool b && b) ? 1 : 0;
}

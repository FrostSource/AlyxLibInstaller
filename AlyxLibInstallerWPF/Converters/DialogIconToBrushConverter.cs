using AlyxLibInstallerShared.Services.Dialog;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AlyxLibInstallerWPF.Converters;
public class DialogIconToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DialogIconType iconType) return Brushes.Transparent;

        return iconType switch
        {
            DialogIconType.Information => (Brush)App.Current.Resources["SystemFillColorAttentionBrush"],
            DialogIconType.Warning => (Brush)App.Current.Resources["SystemFillColorCautionBrush"],
            DialogIconType.Error => (Brush)App.Current.Resources["SystemFillColorCriticalBrush"],
            _ => (Brush)App.Current.Resources["TextFillColorPrimaryBrush"],
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using AlyxLibInstallerShared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AlyxLibInstallerWPF.Converters;

#pragma warning disable WPF0001
public class AppThemeToFluentThemeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AppTheme theme)
        {
            return theme switch
            {
                //AppTheme.None => ThemeMode.None,
                AppTheme.System => ThemeMode.System,
                AppTheme.Light => ThemeMode.Light,
                AppTheme.Dark => ThemeMode.Dark,
                _ => ThemeMode.System
            };
        }

        return ThemeMode.System;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ThemeMode theme)
        {
            if (theme == ThemeMode.System) return AppTheme.System;
            if (theme == ThemeMode.Light) return AppTheme.Light;
            if (theme == ThemeMode.Dark) return AppTheme.Dark;
        }

        return AppTheme.System;
    }
}
#pragma warning disable WPF0001

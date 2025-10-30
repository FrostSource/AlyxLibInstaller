using AlyxLibInstallerShared.Models;
using System.Windows;

namespace AlyxLibInstallerWPF.Extensions;
internal static class ThemeModeExtensions
{
    public static ThemeMode ToThemeMode(this AppTheme theme) => theme switch
    {
        AppTheme.Light => ThemeMode.Light,
        AppTheme.Dark => ThemeMode.Dark,
        AppTheme.System => ThemeMode.System,
        _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, null)
    };

    public static AppTheme ToAppTheme(this ThemeMode mode)
    {
        if (mode == ThemeMode.System) return AppTheme.System;
        if (mode == ThemeMode.Dark) return AppTheme.Dark;
        if (mode == ThemeMode.Light) return AppTheme.Light;
        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
    }
}

using AlyxLibInstallerShared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace AlyxLibInstallerShared.ViewModels;

public partial class AlyxLibInstallerSharedViewModel
{
    [ObservableProperty]
    public partial IList<SelectableAppTheme> SelectableThemes { get; private set; } = null!;

    [ObservableProperty]
    private AppTheme currentTheme;

    public event EventHandler<AppTheme>? OnThemeChanged;

    private bool _isUpdatingTheme = false;

    private void Theme_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isUpdatingTheme) return;

        if (e.PropertyName == nameof(SelectableAppTheme.IsSelected)
            && sender is SelectableAppTheme selectedTheme
            && selectedTheme.IsSelected)
        {
            _isUpdatingTheme = true;

            foreach (var theme in SelectableThemes)
            {
                if (theme != selectedTheme)
                    theme.IsSelected = false;
            }

            CurrentTheme = selectedTheme.Theme;

            _isUpdatingTheme = false;
        }
    }

    partial void OnCurrentThemeChanged(AppTheme value)
    {
        Settings.Theme = value;
        OnThemeChanged?.Invoke(this, value);
        if (_isUpdatingTheme) return;

        _isUpdatingTheme = true;

        // Update the selection to match
        foreach (var theme in SelectableThemes)
        {
            theme.IsSelected = (theme.Theme == value);
        }

        _isUpdatingTheme = false;
    }

    void InitializeThemes()
    {
        SelectableThemes = Enum.GetValues<AppTheme>()
            .Select(x => new SelectableAppTheme(x))
            .ToList();

        foreach (var theme in SelectableThemes)
        {
            theme.PropertyChanged += Theme_PropertyChanged;
        }

        CurrentTheme = Settings.Theme;
        OnCurrentThemeChanged(CurrentTheme);
    }
}

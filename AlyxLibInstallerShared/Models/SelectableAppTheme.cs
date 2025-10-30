using CommunityToolkit.Mvvm.ComponentModel;

namespace AlyxLibInstallerShared.Models;
public partial class SelectableAppTheme(AppTheme theme) : ObservableObject
{
    public AppTheme Theme { get; set; } = theme;

    public string Name => Theme.ToString();

    [ObservableProperty]
    private bool isSelected = false;
}

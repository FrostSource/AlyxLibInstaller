using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Models;
public partial class SelectableAppTheme(AppTheme theme) : ObservableObject
{
    public AppTheme Theme { get; set; } = theme;

    public string Name => Theme.ToString();

    [ObservableProperty]
    private bool isSelected = false;
}

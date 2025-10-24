using CommunityToolkit.Mvvm.ComponentModel;
using Source2HelperLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Models;
public partial class SelectableLocalAddon(LocalAddon addon, bool isSelected = false) : ObservableObject, ISelectable
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; } = isSelected;

    public LocalAddon LocalAddon { get; } = addon;
}

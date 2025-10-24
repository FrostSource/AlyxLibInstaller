using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Models;
public class EditableEntry : INotifyPropertyChanged
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public EditableEntry() { }
    public EditableEntry(string name) => Name = name;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string prop) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    public static implicit operator string(EditableEntry entry) => entry.Name;
    public static implicit operator EditableEntry(string name) => new(name);

}

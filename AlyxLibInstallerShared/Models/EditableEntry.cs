using AlyxLib.Services;
using System.ComponentModel;

namespace AlyxLibInstallerShared.Models;
public class EditableEntry : INotifyPropertyChanged
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            if (IsExcludePattern && IsTemporary)
                IsTemporary = false;

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(IsExcludePattern));
            OnPropertyChanged(nameof(CanBeTemporary));
        }
    }

    private bool _isTemporary = false;
    public bool IsTemporary
    {
        get => _isTemporary;
        set
        {
            _isTemporary = CanBeTemporary && value;
            OnPropertyChanged(nameof(IsTemporary));
        }
    }

    public bool IsExcludePattern => Name.StartsWith('!');
    public bool CanBeTemporary => !IsExcludePattern;

    public EditableEntry() { }
    public EditableEntry(string name) => Name = name;
    public EditableEntry(string name, bool isTemporary)
    {
        Name = name;
        IsTemporary = isTemporary;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string prop) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    public string ToConfigString() => IsTemporary ? $"{FileGlobService.TemporaryPrefix}{Name}" : Name;

    public static implicit operator string(EditableEntry entry) => entry.Name;
    public static implicit operator EditableEntry(string name) => new(name);
}

using AlyxLibInstallerShared.Services.Dialog;
using Microsoft.Win32;
using System.Windows;

namespace AlyxLibInstallerWPF.Services.Interfaces;
public class WpfFolderPickerService : IFolderPickerService
{
    private protected Window? _owner = null;

    public WpfFolderPickerService() { }
    public WpfFolderPickerService(Window? owner = null) => _owner = owner;

    public string? PickFolder(string? title = null, string? initialPath = null)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title ?? "Select folder",
            InitialDirectory = initialPath ?? string.Empty,
        };

        //if (_owner == null && window is null)
        //{
        //    throw new ArgumentException($"{nameof(_owner)} must be a Window", nameof(_owner));
        //}

        bool? result = _owner != null ? dialog.ShowDialog(_owner) : dialog.ShowDialog();
        return result == true ? dialog.FolderName : null;
    }

    public Task<string?> PickFolderAsync(string? title = null, string? initialPath = null)
    {
        return Task.FromResult(PickFolder(title, initialPath));
    }
}

using AlyxLib.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AlyxLibInstallerShared.Models;
public class FileGlobCollection : ObservableCollection<EditableEntry>
{
    public ICommand DeleteEntryCommand { get; private set; } = null!;
    public ICommand AddEntryCommand { get; private set; } = null!;

    void Initialize()
    {
        DeleteEntryCommand = new RelayCommand<EditableEntry>(entry =>
        {
            if (entry != null && Contains(entry))
                Remove(entry);
        });

        AddEntryCommand = new RelayCommand(() =>
        {
            Add(new EditableEntry());
        });
    }

    public FileGlobCollection() : base()
    {
        Initialize();
    }

    public FileGlobCollection(IEnumerable<EditableEntry> entries) : base(entries.Select(e => new EditableEntry(e.Name, e.IsTemporary)))
    {
        Initialize();
    }

    public FileGlobCollection(IEnumerable<string> entries) : base(entries.Select(e =>
    {
        var isTemp = false;
        var name = e;
        if (name.StartsWith(FileGlobService.TemporaryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            isTemp = true;
            name = name[FileGlobService.TemporaryPrefix.Length..];
        }

        return new EditableEntry(name, isTemp);
    }))
    {
        Initialize();
    }

    public string[] GetMatchingFiles(string path)
    {
        var globService = new FileGlobService(this.Select(entry => entry.ToConfigString()));
        return [.. globService.GetMatchingFileActions(path).Select(FormatMatch)];
    }

    public int GetMatchingFileCount(string? path) => path is null ? 0 : GetMatchingFiles(path).Length;

    private static string FormatMatch(FileRemovalMatch match)
    {
        var actionPrefix = match.Action switch
        {
            FileRemovalAction.Delete => "del|",
            FileRemovalAction.TemporaryMove => "tmp|",
            _ => string.Empty
        };

        return $"{actionPrefix}{match.RelativePath}";
    }

    public FileGlobCollection Clone()
    {
        return new(this);
    }
}

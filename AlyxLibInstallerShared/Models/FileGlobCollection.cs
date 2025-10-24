using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

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
        //for (int i = 0; i < 10; i++)
        //    Add(new EditableEntry { Name = $"Test {i}" });

        Initialize();
    }

    public FileGlobCollection(IEnumerable<EditableEntry> entries) : base(entries.Select(e => new EditableEntry(e.Name)))
    {
        Initialize();
    }

    public FileGlobCollection(IEnumerable<string> entries) : base(entries.Select(e => new EditableEntry(e)))
    {
        Initialize();
    }

    public string[] GetMatchingFiles(string path)
    {
        Matcher matcher = new(preserveFilterOrder: true);

        foreach (EditableEntry entry in this)
        {
            if (entry.Name.StartsWith("!"))
                matcher.AddExclude(entry.Name.Substring(1));
            else
                matcher.AddInclude(entry.Name);
        }

        PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(path)));

        return [.. result.Files.Select(f => f.Path)];
    }

    public int GetMatchingFileCount(string? path) => path is null ? 0 : GetMatchingFiles(path).Length;

    public FileGlobCollection Clone()
    {
        return new(this);
    }
}

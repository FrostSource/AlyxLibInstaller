using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared;
public class FolderDeletionMonitor : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly string _folderPath;
    private readonly Action? _onDeleted;
    private readonly Action? _onCreated;
    private bool _disposed;

    public FolderDeletionMonitor(string folderPath, Action? onDeleted = null, Action? onCreated = null)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

        _folderPath = Path.GetFullPath(folderPath);
        _onDeleted = onDeleted ?? throw new ArgumentNullException(nameof(onDeleted));
        _onCreated = onCreated;

        var parentDir = Path.GetDirectoryName(_folderPath);

        // Handle root directories (e.g., "C:\") which have no parent
        if (string.IsNullOrEmpty(parentDir))
            throw new ArgumentException("Cannot monitor root directories", nameof(folderPath));

        if (!Directory.Exists(parentDir))
            throw new DirectoryNotFoundException($"Parent directory not found: {parentDir}");

        var folderName = Path.GetFileName(_folderPath);

        _watcher = new FileSystemWatcher(parentDir)
        {
            Filter = folderName,
            NotifyFilter = NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };

        if (_onDeleted != null)
            _watcher.Deleted += OnFolderDeleted;
        
        _watcher.Renamed += OnFolderRenamed;

        if (_onCreated != null)
        {
            _watcher.Created += OnFolderCreated;
        }
    }

    private void OnFolderDeleted(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Equals(_folderPath, StringComparison.OrdinalIgnoreCase))
        {
            _onDeleted?.Invoke();
        }
    }

    private void OnFolderRenamed(object sender, RenamedEventArgs e)
    {
        // If our folder was renamed to something else, treat it as deleted
        if (e.OldFullPath.Equals(_folderPath, StringComparison.OrdinalIgnoreCase))
        {
            _onDeleted?.Invoke();
        }
        // If something was renamed to our folder name, treat it as created
        else if (e.FullPath.Equals(_folderPath, StringComparison.OrdinalIgnoreCase))
        {
            _onCreated?.Invoke();
        }
    }

    private void OnFolderCreated(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Equals(_folderPath, StringComparison.OrdinalIgnoreCase))
        {
            _onCreated?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_watcher != null)
        {
            _watcher.Deleted -= OnFolderDeleted;
            _watcher.Created -= OnFolderCreated;
            _watcher.Renamed -= OnFolderRenamed;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }

        _disposed = true;
    }

    public static FolderDeletionMonitor Either(string folderPath, Action onChanged) => new(folderPath, onChanged, onChanged);
}
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace AlyxLib.Services;

public class FileGlobService
{
    private readonly IEnumerable<string> _globs;

    public event Action<string, Exception>? OnFileDeleteException;
    public event Action<string>? OnFileDelete;

    public FileGlobService(IEnumerable<string> globs)
    {
        _globs = globs;
    }

    public IEnumerable<string> GetMatchingFiles(string dir)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

        foreach (var pattern in _globs)
        {
            if (pattern.StartsWith('!'))
                matcher.AddExclude(pattern[1..]);
            else
                matcher.AddInclude(pattern);
        }

        var directoryInfoWrapper = new DirectoryInfoWrapper(new DirectoryInfo(dir));
        return matcher.Execute(directoryInfoWrapper).Files.Select(f => Path.Combine(dir, f.Path));
    }

    public int GetMatchingFileCount(string dir) => GetMatchingFiles(dir).Count();

    public void DeleteMatchingFiles(string dir)
    {
        foreach (var file in GetMatchingFiles(dir))
        {
            try
            {
                File.Delete(file);
                OnFileDelete?.Invoke(file);
            }
            catch (Exception ex)
            {
                OnFileDeleteException?.Invoke(file, ex);
            }
        }
    }
}

#nullable enable

using System.IO;

namespace AlyxLib;

public class PathInfo
{
    public FileSystemInfo Info;

    public PathInfo(string path)
    {
        if (Directory.Exists(path))
        //FileAttributes attr = File.GetAttributes(path);

        //if (attr.HasFlag(FileAttributes.Directory))
        {
            Info = new DirectoryInfo(path);
            IsDirectory = true;
        }
        else
        {
            Info = new FileInfo(path);
            IsDirectory = false;
        }
    }

    public bool Exists => Info.Exists;

    public string Name => Info.Name;

    public string FullName => Info.FullName;

    public bool IsDirectory { get; private set; }

    public string? LinkTarget => Info.LinkTarget;
    
    public static string operator /(PathInfo left, string right)
    {
        return Path.Combine(left.FullName, right);
    }
    public static string operator /(PathInfo left, FileSystemInfo right)
    {
        return Path.Combine(left.FullName, right.FullName);
    }

    public override string ToString() {  return Info.ToString(); }
}

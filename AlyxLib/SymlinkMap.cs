#nullable enable

using Source2HelperLibrary;
using System;
using System.IO;

namespace AlyxLib;

public class SymlinkMap
{
    /// <summary>
    /// The relative file/folder in the AlyxLib directory that will be linked to.
    /// </summary>
    public string From;
    /// <summary>
    /// The relative file/folder in the addon directory that will link to <see cref="From"/>.
    /// </summary>
    public string To;
    public bool IsContentFile = true;
    public bool RemoveOnUpload = false;
    public bool IsSymbolicLink = true;

    //private PathInfo? toInfo;

    public SymlinkMap(string from, string to, bool isContentFile, bool removeOnUpload)
    {
        From = from;
        To = to;
        IsContentFile = isContentFile;
        RemoveOnUpload = removeOnUpload;
    }

    public SymlinkMap(string from, string to)
    {
        From = from;
        To = to;
    }

    public SymlinkMap(string from)
    {
        From = from;
        To = from;
    }

    public string GetFullPath(LocalAddon addon)
    {
        if (IsContentFile)
            return Path.Combine(addon.ContentPath, To);
        else
        {
            if (addon.GamePath == null)
                throw new Exception($"Addon {addon.Name} doesn't have a game path but {To} expects it to exist!");
            return Path.Combine(addon.GamePath, To);
        }
    }

    //public void SetAddon(LocalAddon addon)
    //{
    //    string toDir;
    //    if (IsContentFile)
    //    {
    //        toDir = addon.ContentPath;
    //    }
    //    else
    //    {
    //        if (addon.GamePath == null)
    //        {
    //            throw new Exception($"Addon {addon.Name} doesn't have a game extractPath but {To} expects it to exist!");
    //        }
    //        toDir = addon.GamePath;
    //    }
    //    toInfo = new PathInfo(Path.Combine(toDir, To));
    //}

    //public bool Exists
    //{
    //    get
    //    {
    //        if (toInfo == null) return false;
    //        return toInfo.Exists;
    //    }
    //}

    //public bool ExistsAsSymbolicLink
    //{
    //    get
    //    {
    //        if (toInfo == null) return false;
    //        return toInfo.LinkTarget != null;
    //    }
    //}

    //public bool ExistsAsSymbolicLinkAndPointsToToPath
    //{
    //    get
    //    {
    //        return toInfo.LinkTarget == (AlyxLibPath / file.From)
    //    }
    //}
}

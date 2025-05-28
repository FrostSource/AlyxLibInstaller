#nullable enable

using Microsoft.UI.Xaml.Controls;
using Source2HelperLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Semver;
using System.Diagnostics.CodeAnalysis;
using LibGit2Sharp;
using Windows.Media.Capture;
using System.Security.Cryptography;

namespace AlyxLibInstaller.AlyxLib;

public partial class AlyxLibManager
{
    public readonly VersionManager VersionManager;
    public readonly FileManager FileManager;

    public PathInfo? AlyxLibPath { get; private set; } = null;

    [MemberNotNullWhen(true, nameof(AlyxLibPath))]
    public bool AlyxLibExists
    {
        get => AlyxLibPath != null && AlyxLibPath.Exists;
    }

    public string AlyxLibVersion { get; private set; } = "0.0.0";

    public AlyxLibManager()
    {
        VersionManager = new(this);
        FileManager = new(this);
    }

    public void SetAlyxLibPath(string path)
    {
        AlyxLibPath = new PathInfo(path);
        AlyxLibVersion = VersionManager.TryGetLocalVersion(out var version) ? version : "0.0.0";
        SettingsManager.Settings.AlyxLibDirectory = AlyxLibPath.FullName;
    }

    [MemberNotNullWhen(false, nameof(AlyxLibPath))]
    public bool IssueFound()
    {
        if (AlyxLibPath == null || !AlyxLibExists)
        {
            App.DebugConsoleError("AlyxLib path not set");
            return true;
        }

        return false;
    }
}

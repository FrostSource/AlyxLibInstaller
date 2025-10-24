using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared;
public static class ShellContextMenuRegistrar
{
    private const string ContextMenuKey = @"Software\Classes\Directory\shell\AlyxLibInstaller";
    private const string ContextMenuCommandKey = @"Software\Classes\Directory\shell\AlyxLibInstaller\command";
    private const string BackgroundContextMenuKey = @"Software\Classes\Directory\Background\shell\AlyxLibInstaller";
    private const string BackgroundContextMenuCommandKey = @"Software\Classes\Directory\Background\shell\AlyxLibInstaller\command";

    [SupportedOSPlatform("windows")]
    public static void RegisterExplorerContextMenu(string text)
    {
        string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrEmpty(exePath))
            throw new InvalidOperationException("Could not determine the executable path for the current process.");

        // Folder context menu
        using (var key = Registry.CurrentUser.CreateSubKey(ContextMenuKey))
        {
            key.SetValue("", text);
            key.SetValue("Icon", exePath);
        }
        using (var commandKey = Registry.CurrentUser.CreateSubKey(ContextMenuCommandKey))
        {
            commandKey.SetValue("", $"\"{exePath}\" \"%V\"");
        }

        // Background context menu
        using (var key = Registry.CurrentUser.CreateSubKey(BackgroundContextMenuKey))
        {
            key.SetValue("", text);
            key.SetValue("Icon", exePath);
        }
        using (var commandKey = Registry.CurrentUser.CreateSubKey(BackgroundContextMenuCommandKey))
        {
            commandKey.SetValue("", $"\"{exePath}\" \"%V\"");
        }
    }

    [SupportedOSPlatform("windows")]
    public static void UnregisterExplorerContextMenu()
    {
        Registry.CurrentUser.DeleteSubKeyTree(ContextMenuKey, false);
        Registry.CurrentUser.DeleteSubKeyTree(BackgroundContextMenuKey, false);
    }

    [SupportedOSPlatform("windows")]
    public static bool IsExplorerContextMenuRegistered()
    {
        using var folderKey = Registry.CurrentUser.OpenSubKey(ContextMenuKey);
        using var backgroundKey = Registry.CurrentUser.OpenSubKey(BackgroundContextMenuKey);

        return folderKey != null && backgroundKey != null;
    }

}

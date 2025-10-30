using System.Windows;

///
/// This is all old stuff that needs to be deleted when viewmodel is set up.
///

namespace AlyxLibInstallerWPF;
public partial class MainWindow
{
    private async void MenuBarNewAddon_Click(object sender, RoutedEventArgs e)
    {
        //var result = await DialogHelper.ShowCustomPopupAsync(this, new NewAddonDialog(), "Creating New Addon", "Create", "Cancel", "");
        //if (result.Result == ContentDialogResult.Primary)
        //{
        //    var addonName = result.Data?.AddonName;

        //    if (string.IsNullOrEmpty(addonName) || !AlyxLibHelpers.StringIsValidModName(addonName))
        //    {
        //        DebugConsoleError($"'{addonName}' is not a valid addon name!");
        //        return;
        //    }

        //    var contentPath = HLA.GetAddonContentFolder();
        //    if (Directory.Exists(contentPath))
        //    {
        //        Directory.CreateDirectory(Path.Combine(contentPath, addonName));
        //        DebugConsoleMessage("Created content folder.");
        //    }
        //    else
        //    {
        //        DebugConsoleWarning("Could not find content path!", $"Could not find content path: '{contentPath}'");
        //        DebugConsoleWarning($"Addon '{addonName}' was not created");
        //        return;
        //    }

        //    var gamePath = HLA.GetAddonGameFolder();
        //    if (Directory.Exists(gamePath))
        //    {
        //        Directory.CreateDirectory(Path.Combine(gamePath, addonName));
        //        DebugConsoleMessage("Created game folder");
        //    }
        //    else
        //    {
        //        DebugConsoleWarning("Could not find game path!", $"Could not find game path: '{gamePath}'");
        //    }

        //    DebugConsoleSuccess($"Successfully created addon '{addonName}'!");
        //    SelectAddon(addonName);
        //}
    }

    private void MenuBarOpenAddon_Click(object sender, RoutedEventArgs e)
    {
        //DebugConsoleVerbose("Starting addon folder selection");
        //var folder = DialogHelper.FolderPicker(this, HLA.GetAddonContentFolder());

        //if (folder == null)
        //{
        //    DebugConsoleVerbose("User cancelled addon folder selection");
        //    return;
        //}

        ////var name = Path.GetFileName(Path.GetDirectoryName(folder.Path));
        //var name = Path.GetFileName(folder);
        //if (name == null)
        //{
        //    DebugConsoleVerbose($"Path is invalid: {folder}");
        //    return;
        //}

        //SelectAddon(name);
    }

    private async void MenuBarManageRemoveList_Click(object sender, RoutedEventArgs e)
    {
        //TODO: Create ManageRemovalListDialog
        //await DialogHelper.ShowCustomPopupAsync(this, new ManageRemovalListDialog(), "Manage Removal List", "Done");
    }
}

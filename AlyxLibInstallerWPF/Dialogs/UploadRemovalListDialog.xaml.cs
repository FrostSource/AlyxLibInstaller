using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services.Dialog;
using Source2HelperLibrary;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace AlyxLibInstallerWPF.Dialogs;
/// <summary>
/// Interaction logic for UploadRemovalListView.xaml
/// </summary>
public partial class UploadRemovalListView : UserControl
{
    public FileGlobCollection GlobCollection { get; }
    public LocalAddon LocalAddon { get; }
    public IDialogService DialogService { get; }

    public UploadRemovalListView(FileGlobCollection globCollection, LocalAddon localAddon, IDialogService dialogService)
    {
        InitializeComponent();

        GlobCollection = globCollection;
        LocalAddon = localAddon;
        DialogService = dialogService;
        DataContext = GlobCollection;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var files = GlobCollection.GetMatchingFiles(LocalAddon.GamePath);

        if (GlobCollection.Count == 0 || files.Length == 0)
        {
            DialogService.ShowTextPopup(new DialogConfiguration
            {
                Title = "File Removal List",
                Message = GlobCollection.Count == 0
                    ? "No file patterns have been added to the removal list. No files will be removed."
                    : "No files matched by patterns in the removal list. No files will be removed.",
                CancelButtonText = "Close"
            });
        }
        else
        {
            DialogService.ShowListPopup(new DialogConfiguration
            {
                Title = "File Removal List",
                Message = "These files will be removed from the 'game' directory of this addon when clicking 'Remove For Upload'",
                CancelButtonText = "Close"
            }, files);
        }
    }
}

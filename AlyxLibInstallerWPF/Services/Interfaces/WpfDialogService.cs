using AlyxLibInstallerShared;
using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services.Dialog;
using AlyxLibInstallerWPF.ViewModels;
using AlyxLibInstallerWPF.Dialogs;
using Source2HelperLibrary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlyxLibInstallerWPF.Services.Interfaces;
public class WpfDialogService : WpfFolderPickerService, IDialogService
{

    public WpfDialogService() { }
    public WpfDialogService(Window owner) => _owner = owner;

    public async Task<DialogResult> ShowCustomPopup<T>(T content, DialogConfiguration config, Panel? panel = null) where T : UIElement
    {
        var dialog = new ContentDialogWpfNew
        {
            DialogContent = content,
            DataContext = config
        };

        panel ??= _owner?.Content as Panel;

        if (panel == null)
            throw new InvalidOperationException("You must set the owner of the dialog to a panel");

        if (_owner is MainWindow mw)
        {
            KeyboardNavigation.SetTabNavigation(mw.RootGrid, KeyboardNavigationMode.None);
        }

        var result = await panel.Dispatcher.InvokeAsync(() => DialogQueue.ShowDialogAsync(dialog, panel));

        // InvokeAsync returns a Task<ContentDialogResult>, so await it again
        return await result switch
        {
            ContentDialogResult.Primary => DialogResult.Primary,
            ContentDialogResult.Secondary => DialogResult.Secondary,
            ContentDialogResult.None => DialogResult.None,
            _ => throw new NotImplementedException()
        };
    }

    public async Task<DialogResponse> ShowTextPopup(DialogConfiguration config)
    {
        //return await ShowCustomPopup(new TextBlock { Text = config.Message }, new DialogConfiguration
        //{
        //    PrimaryButtonText = config.PrimaryButtonText,
        //    SecondaryButtonText = config.SecondaryButtonText,
        //    CancelButtonText = config.CancelButtonText,
        //    Title = config.Title,
        //});

        var vm = new PromptDialogViewModel(config);

        var dialog = new PromptDialog() { DataContext = vm };

        var result = await ShowCustomPopup(dialog, config);

        return new DialogResponse(
            result,
            config.HasCheckBox ? vm.IsCheckBoxChecked : null,
            config.HasTextBox ? vm.TextBoxText : null
            );

        //return await ShowCustomPopup(new TextBlock { Text = config.Message }, config);
        //return new DialogResponse(DialogResult.None);
    }

    public async Task<DialogResponse> ShowAboutPopup(DialogConfiguration config, AboutInfo info)
    {
        var dialog = new AboutView() { DataContext = info };

        var result = await ShowCustomPopup(dialog, config);

        return new DialogResponse(result);
    }

    public async Task<DialogResponse> ShowWarningPopup(DialogConfiguration config)
    {
        ///TODO Replace with custom dialog
        //System.Windows.MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        //DialogHelper.ShowCustomPopupAsync(_owner, new SimpleTextDialog(message), title);
        //var result = await ShowCustomPopup(new TextBlock { Text = message }, new DialogConfiguration { Title = title, CancelButtonText = "OK" });

        var response = await ShowTextPopup(config);
        return response;
    }

    public async Task<DialogResponse> ShowPrivilegeWarning()
    {
        //return await ShowCustomPopup(new PrivilegeWarningDialog(), new DialogConfiguration());
        var result = await ShowCustomPopup(new PrivilegeWarningDialog(), new DialogConfiguration { CancelButtonText = "OK" });
        return new DialogResponse(result);
    }

    public async Task<DialogResponse> ShowIntroAlyxLibPopup()
    {
        var msg = "The AlyxLib files must exist on your computer so they can be linked to your addon, and it is recommended that you download to the same folder your addon exist in.\r\n\r\nWould you like to select a download location or select an already downloaded AlyxLib?";
        var result = await ShowCustomPopup(new TextBlock { Text = msg, TextAlignment = TextAlignment.Center }, new DialogConfiguration
        {
            Title = "Welcome to the AlyxLib installer!",
            PrimaryButtonText = "Download AlyxLib",
            SecondaryButtonText = "Select AlyxLib Folder",
            CancelButtonText = "Cancel"
        });

        return new DialogResponse(result);


        //result = await DialogHelper.ShowCustomPopupAsync(this,
        //    new SimpleTextDialog("Welcome to the AlyxLib installer! The AlyxLib files must exist on your computer so they can be linked to your addon, and it is recommended that you download to the same folder your addon exist in.\r\n\r\nWould you like to select a download location or select an already downloaded AlyxLib?"),
        //    "Setup AlyxLib", "Download AlyxLib", "Select AlyxLib Folder", "Cancel");

        //switch (result.Result)
        //{
        //    case ContentDialogResult.Primary:
        //        DownloadAlyxLib();
        //        break;

        //    case ContentDialogResult.Secondary:
        //        PromptUserToSelectAlyxLibFolder();
        //        break;
        //}
    }

    public async Task<DialogResponse> ShowFileRemovalPopup(DialogConfiguration config, FileGlobCollection globCollection, LocalAddon addon)
    {
        var result = await ShowCustomPopup(new UploadRemovalListView(globCollection, addon, this), config);
        return new DialogResponse(result);
    }

    public async Task<DialogResponse> ShowListPopup(DialogConfiguration config, IEnumerable<string> list, Panel? panel = null)
    {
        var result = await ShowCustomPopup(new StringListView { StringList = list, Message = config.Message }, config, panel);
        return new DialogResponse(result);
    }

    public async Task<DialogResponse> ShowListPopup(DialogConfiguration config, IEnumerable<string> list)
    {
        return await ShowListPopup(config, list, null);
    }
}

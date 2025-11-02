using AlyxLibInstallerShared;
using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services.Dialog;
using AlyxLibInstallerWPF.Dialogs;
using AlyxLibInstallerWPF.ViewModels;
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
        config = DialogConfiguration.Defaults.With(config);

        var vm = new PromptDialogViewModel(config);

        var dialog = new PromptDialog() { DataContext = vm };

        var result = await ShowCustomPopup(dialog, config);

        return new DialogResponse(
            result,
            config.HasCheckBox ?? false ? vm.IsCheckBoxChecked : null,
            config.HasTextBox ?? false ? vm.TextBoxText : null
            );
    }

    public async Task<DialogResponse> ShowAboutPopup(DialogConfiguration config, AboutInfo info)
    {
        config = DialogConfiguration.Defaults.With(config);

        var dialog = new AboutView() { DataContext = info };

        var result = await ShowCustomPopup(dialog, config);

        return new DialogResponse(result);
    }

    private readonly DialogConfiguration warningConfig =
        DialogConfiguration.Defaults with
        {
            Title = "Warning",
            IconType = DialogIconType.Warning,
            Width = 400
        };

    public async Task<DialogResponse> ShowWarningPopup(DialogConfiguration config)
    {
        var response = await ShowTextPopup(warningConfig.With(config));
        return response;
    }

    public async Task<DialogResponse> ShowPrivilegeWarning()
    {
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

using AlyxLibInstallerShared.Models;
using Source2HelperLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Services.Dialog;
public interface IDialogService : IFolderPickerService
{
    /// <summary>
    /// Prompts the user for input.
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="config"></param>
    /// <returns>The string input, or null if the user cancelled.</returns>
    //string? PromptStringResult(DialogConfiguration config);
    Task<DialogResponse> ShowIntroAlyxLibPopup();
    Task<DialogResponse> ShowPrivilegeWarning();
    Task<DialogResponse> ShowTextPopup(DialogConfiguration config);
    Task<DialogResponse> ShowFileRemovalPopup(DialogConfiguration config, FileGlobCollection globCollection, LocalAddon addon);
    Task<DialogResponse> ShowAboutPopup(DialogConfiguration config, AboutInfo info);
    Task<DialogResponse> ShowListPopup(DialogConfiguration config, IEnumerable<string> list);
    Task<DialogResponse> ShowWarningPopup(DialogConfiguration config);
    Task<DialogResponse> ShowWarningPopup(string message) => ShowWarningPopup(new DialogConfiguration { Title = "Warning", Message = message, IconType = DialogIconType.Warning });
    Task<DialogResponse> ShowWarningPopup(string title, string message) => ShowWarningPopup(new DialogConfiguration { Title = title, Message = message, IconType = DialogIconType.Warning });
}

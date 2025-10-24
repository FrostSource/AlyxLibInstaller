using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Services.Dialog;
public interface IFolderPickerService
{
    string? PickFolder(string? title = null, string? initialPath = null);

    Task<string?> PickFolderAsync(string? title = null, string? initialPath = null);
}

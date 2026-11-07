using AlyxLib;
using AlyxLibInstallerShared;
using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services;
using AlyxLibInstallerShared.Services.Dialog;
using AlyxLibInstallerShared.ViewModels;
using Source2HelperLibrary;
using System.Runtime.CompilerServices;

namespace AlyxLibInstallerTests;

/// <summary>
/// Dialog service that returns queued responses instead of showing anything,
/// and records every dialog that would have been shown.
/// </summary>
internal class FakeDialogService : IDialogService
{
    private readonly Queue<DialogResponse> _responses = new();

    public List<(string Method, DialogConfiguration? Config)> Shown { get; } = [];

    public string? PickFolderResult { get; set; }
    public string? PickFileResult { get; set; }

    /// <summary>
    /// Runs while a popup with a view model is showing, standing in for the user's edits.
    /// </summary>
    public Action<object>? OnPopupShown { get; set; }
    public int PickFolderCount { get; private set; }

    public void Respond(DialogResult result, bool? checkboxChecked = null, string? inputText = null) =>
        _responses.Enqueue(new DialogResponse(result, checkboxChecked, inputText));

    private Task<DialogResponse> Show(string method, DialogConfiguration? config = null)
    {
        Shown.Add((method, config));
        var response = _responses.Count > 0 ? _responses.Dequeue() : new DialogResponse(DialogResult.None);
        // Like the real dialog, an untouched text box returns the text it started with
        if (config?.HasTextBox == true && response.InputText == null)
            response = response with { InputText = config.TextBoxDefaultText ?? "" };
        return Task.FromResult(response);
    }

    public Task<DialogResponse> ShowIntroAlyxLibPopup() => Show(nameof(ShowIntroAlyxLibPopup));
    public Task<DialogResponse> ShowPrivilegeWarning() => Show(nameof(ShowPrivilegeWarning));
    public Task<DialogResponse> ShowTextPopup(DialogConfiguration config) => Show(nameof(ShowTextPopup), config);
    public Task<DialogResponse> ShowFileRemovalPopup(DialogConfiguration config, FileGlobCollection globCollection, LocalAddon addon) => Show(nameof(ShowFileRemovalPopup), config);
    public Task<DialogResponse> ShowAboutPopup(DialogConfiguration config, AboutInfo info) => Show(nameof(ShowAboutPopup), config);
    public Task<DialogResponse> ShowListPopup(DialogConfiguration config, IEnumerable<string> list) => Show(nameof(ShowListPopup), config);
    public Task<DialogResponse> ShowWarningPopup(DialogConfiguration config) => Show(nameof(ShowWarningPopup), config);

    public Task<DialogResponse> ShowWorkshopPropertiesPopup(DialogConfiguration config, WorkshopPropertiesEditorViewModel editor)
    {
        OnPopupShown?.Invoke(editor);
        return Show(nameof(ShowWorkshopPropertiesPopup), config);
    }

    public Task<DialogResponse> ShowChangeLinkedItemPopup(DialogConfiguration config, ChangeLinkedItemViewModel picker)
    {
        OnPopupShown?.Invoke(picker);
        return Show(nameof(ShowChangeLinkedItemPopup), config);
    }

    public string? PickFolder(string? title = null, string? initialPath = null)
    {
        PickFolderCount++;
        return PickFolderResult;
    }

    public Task<string?> PickFolderAsync(string? title = null, string? initialPath = null) => Task.FromResult(PickFolder(title, initialPath));

    public string? PickFile(string? title = null, string? filter = null, string? initialPath = null) => PickFileResult;
}

/// <summary>
/// Addon file operations that record calls instead of touching the disk.
/// </summary>
internal class FakeAddonFileOperations : IAddonFileOperations
{
    public AddonConfig InstalledConfig { get; set; } = new();
    public bool GitRepository { get; set; }
    public Exception? InstallException { get; set; }

    public List<string> Calls { get; } = [];
    public bool? RemovedGitRepository { get; private set; }

    public AddonConfig GetInstalledConfig(LocalAddon addon) => InstalledConfig;

    public bool HasGitRepository(LocalAddon addon) => GitRepository;

    public void Install(LocalAddon addon, AddonConfig options)
    {
        Calls.Add(nameof(Install));
        if (InstallException != null)
            throw InstallException;
    }

    public void RemoveForUpload(LocalAddon addon, AddonConfig options) => Calls.Add(nameof(RemoveForUpload));

    public void UninstallOptions(LocalAddon addon, AddonConfig options) => Calls.Add(nameof(UninstallOptions));

    public void UninstallFully(LocalAddon addon, bool removeGitRepository)
    {
        Calls.Add(nameof(UninstallFully));
        RemovedGitRepository = removeGitRepository;
    }
}

internal static class TestAddon
{
    /// <summary>
    /// Creates an addon without running its constructor, which looks up the real game install.
    /// </summary>
    public static LocalAddon Create() => (LocalAddon)RuntimeHelpers.GetUninitializedObject(typeof(LocalAddon));
}

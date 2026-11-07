using AlyxLib.Logging;
using AlyxLib.Workshop;
using AlyxLibInstallerShared.Services.Dialog;
using AlyxLibInstallerShared.ViewModels;

namespace AlyxLibInstallerShared.Services;

/// <summary>
/// Edits an addon's Workshop item properties, saving them in the addon and updating the Workshop where the user chooses.
/// </summary>
/// <param name="connect">Opens a Workshop connection, which is closed as soon as it's no longer needed.</param>
/// <param name="setLoading">Shows or hides the loading overlay with a status message.</param>
public class WorkshopPropertiesService(
    IDialogService dialogService,
    Func<IWorkshopClient> connect,
    WorkshopItemStore store,
    Action<bool, string> setLoading,
    ILogger? logger = null,
    Action<Exception>? logException = null)
{
    private readonly Action<Exception> _logException = logException ?? FileLogger.Log;
    private readonly WorkshopLinkService _links = new(dialogService, store, logger);

    /// <returns>True if the properties were saved.</returns>
    public async Task<bool> EditAsync(string addonName, string contentPath)
    {
        try
        {
            return await EditCoreAsync(addonName, contentPath);
        }
        catch (Exception ex)
        {
            setLoading(false, "");
            logger?.LogError("Editing Workshop properties failed.");
            _logException(ex);
            return false;
        }
    }

    private async Task<bool> EditCoreAsync(string addonName, string contentPath)
    {
        var properties = await _links.LoadOrLinkAsync(addonName, contentPath);
        if (properties == null)
            return false;

        if (!properties.HasItem)
            return await EditNewItemAsync(addonName, contentPath, properties);

        WorkshopItemDetails? remote = null;
        string? remoteError = null;
        try
        {
            setLoading(true, "Checking Workshop item...");
            using var client = connect();
            remote = await client.GetItemAsync(properties.ItemId);
            if (remote == null)
                remoteError = $"Workshop item {properties.ItemId} doesn't exist. It may have been deleted.";
        }
        catch (WorkshopException ex)
        {
            remoteError = ex.Message;
        }
        finally
        {
            setLoading(false, "");
        }

        // Without the Workshop's values there's nothing to start untracked properties from
        if (remote == null && HasUntrackedProperties(properties))
        {
            await dialogService.ShowWarningPopup("Can't Load Item Properties",
                $"{remoteError}\n\nThe item's current properties are needed the first time they're edited here.");
            return false;
        }

        var editor = new WorkshopPropertiesEditorViewModel(properties, contentPath, remote, remoteError, dialogService);
        if (!await ShowEditorAsync(dialogService, editor, "Item Properties", "Save"))
            return false;

        var updated = editor.Build();
        updated.Save(contentPath);
        logger?.LogInfo($"Saved Workshop properties for {addonName}");

        var fields = editor.FieldsToUpdate;
        if (fields == WorkshopFields.None && !editor.PreviewChanged)
            return true;

        try
        {
            setLoading(true, "Connecting to Steam...");
            using var client = connect();

            var update = new WorkshopItemUpdate(updated.ItemId)
            {
                PreviewFile = editor.PreviewChanged && updated.PreviewFile != null ? WorkshopProperties.ResolvePath(contentPath, updated.PreviewFile) : null
            }.WithProperties(updated, editor.CurrentDescription, fields);

            setLoading(true, "Updating Workshop item...");
            var progress = new Progress<WorkshopUploadProgress>(p =>
                setLoading(true, p.Fraction is double fraction ? $"{p.Stage}... {fraction:P0}" : $"{p.Stage}..."));
            var result = await client.SubmitUpdateAsync(update, progress);
            setLoading(false, "");

            logger?.Log($"Updated Workshop item {updated.ItemId}", LogType.Success);
            if (result.NeedsLegalAgreement)
            {
                await dialogService.ShowWarningPopup("Workshop Legal Agreement",
                    $"The changes were saved, but the item stays hidden until you accept the Steam Workshop legal agreement:\n{WorkshopUploadService.LegalAgreementUrl}");
            }
        }
        catch (WorkshopException ex)
        {
            setLoading(false, "");
            logger?.LogError(ex.Message);
            await dialogService.ShowWarningPopup("Workshop Update Failed", $"{ex.Message}\n\nYour changes were saved in this addon, so you can update the Workshop later.");
        }

        return true;
    }

    /// <summary>
    /// Edits the properties of an item that hasn't been created yet. They're only saved in the addon until it's uploaded.
    /// </summary>
    private async Task<bool> EditNewItemAsync(string addonName, string contentPath, WorkshopProperties properties)
    {
        var editor = new WorkshopPropertiesEditorViewModel(properties, contentPath, null, null, dialogService);
        if (!await ShowEditorAsync(dialogService, editor, "New Workshop Item", "Save"))
            return false;

        editor.Build().Save(contentPath);
        logger?.LogInfo($"Saved Workshop properties for {addonName}. Upload it to create the Workshop item.");
        return true;
    }

    /// <summary>
    /// Shows the properties editor until the user cancels or enters valid properties.
    /// </summary>
    /// <returns>True if the user confirmed valid properties.</returns>
    internal static async Task<bool> ShowEditorAsync(IDialogService dialogService, WorkshopPropertiesEditorViewModel editor, string title, string confirmText)
    {
        while (true)
        {
            var response = await dialogService.ShowWorkshopPropertiesPopup(new DialogConfiguration
            {
                Title = title,
                PrimaryButtonText = confirmText,
                CancelButtonText = "Cancel",
                Width = 560
            }, editor);
            if (response.Result != DialogResult.Primary)
                return false;

            var problem = editor.Validate();
            if (problem == null)
                return true;

            await dialogService.ShowWarningPopup("Check Item Properties", problem);
        }
    }

    private static bool HasUntrackedProperties(WorkshopProperties properties) =>
        properties.Title == null
        || (properties.Description == null && !properties.HasDescriptionFile)
        || properties.Visibility == null
        || properties.Tags == null
        || properties.RequiredItems == null;
}

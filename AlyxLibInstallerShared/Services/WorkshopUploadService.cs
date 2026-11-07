using AlyxLib.Logging;
using AlyxLib.Workshop;
using AlyxLibInstallerShared.Services.Dialog;

namespace AlyxLibInstallerShared.Services;

/// <param name="AddonName">The addon folder name, saved in publish_data.txt like the Workshop tools do.</param>
/// <param name="RemovalGlobs">The addon's upload removal rules.</param>
/// <param name="ExcludedPaths">AlyxLib files and folders to leave out of the pack.</param>
public record WorkshopUploadRequest(
    string AddonName,
    string ContentPath,
    string GamePath,
    IReadOnlyList<string> RemovalGlobs,
    IReadOnlyList<string> ExcludedPaths);

public enum WorkshopUploadResult
{
    Uploaded,
    Cancelled,
    Failed,
    /// <summary>
    /// The user chose to open Item Properties to sort out properties that don't match the Workshop.
    /// </summary>
    OpenProperties
}

/// <summary>
/// Packs an addon and uploads it as an update to its linked Workshop item, without changing any files in the addon.
/// </summary>
/// <param name="connect">Opens a Workshop connection, which is closed when the upload ends.</param>
/// <param name="setLoading">Shows or hides the loading overlay with a status message.</param>
public class WorkshopUploadService(
    IDialogService dialogService,
    Func<IWorkshopClient> connect,
    WorkshopItemStore store,
    VpkPacker packer,
    Action<bool, string> setLoading,
    ILogger? logger = null,
    Action<Exception>? logException = null)
{
    public const string LegalAgreementUrl = "https://steamcommunity.com/sharedfiles/workshoplegalagreement";

    private readonly Action<Exception> _logException = logException ?? FileLogger.Log;
    private readonly WorkshopLinkService _links = new(dialogService, store, logger);

    /// <summary>
    /// Uploads the addon to its linked Workshop item, asking for change notes first.
    /// If the item's properties on the Workshop don't match the local ones, the user chooses whether to update them too.
    /// </summary>
    public async Task<WorkshopUploadResult> UploadAsync(WorkshopUploadRequest request)
    {
        var properties = await _links.LoadOrLinkAsync(request.AddonName, request.ContentPath);
        if (properties == null)
            return WorkshopUploadResult.Cancelled;

        string? localDescription;
        try
        {
            localDescription = properties.ReadDescription(request.ContentPath);
        }
        catch (FileNotFoundException ex)
        {
            await dialogService.ShowWarningPopup("Description File Missing",
                $"The linked description file couldn't be found:\n{ex.FileName}\n\nLink another file in Item Properties, or restore this one, then try again.");
            return WorkshopUploadResult.Failed;
        }

        try
        {
            setLoading(true, "Connecting to Steam...");
            using var client = connect();

            setLoading(true, "Checking Workshop item...");
            var remote = await client.GetItemAsync(properties.ItemId);
            setLoading(false, "");

            if (remote == null)
            {
                await dialogService.ShowWarningPopup("Workshop Item Not Found",
                    $"Workshop item {properties.ItemId} doesn't exist. It may have been deleted. Use Change Linked Item to link another.");
                return WorkshopUploadResult.Failed;
            }

            var differences = WorkshopPropertyComparer.GetDifferences(properties, localDescription, remote);
            bool updateProperties = false;
            if (differences != WorkshopFields.None)
            {
                switch (await AskAboutMismatch(remote, differences))
                {
                    case DialogResult.Secondary:
                        updateProperties = true;
                        break;
                    case DialogResult.Tertiary:
                        return WorkshopUploadResult.OpenProperties;
                    case DialogResult.None:
                        logger?.LogDetail("Upload cancelled");
                        return WorkshopUploadResult.Cancelled;
                }
            }

            var changeNotes = await dialogService.ShowTextPopup(new DialogConfiguration
            {
                Title = $"Upload {remote.Title}",
                Message = "Describe what changed in this update. Change notes are shown on the item's Workshop page.",
                HasTextBox = true,
                TextBoxIsMultiline = true,
                TextBoxPlaceholderText = "Change notes (optional)",
                PrimaryButtonText = "Upload",
                CancelButtonText = "Cancel",
                Width = 450
            });
            if (changeNotes.Result != DialogResult.Primary)
            {
                logger?.LogDetail("Upload cancelled");
                return WorkshopUploadResult.Cancelled;
            }

            setLoading(true, "Packing addon...");
            await Task.Run(() => Pack(request, properties.ItemId));

            var update = new WorkshopItemUpdate(properties.ItemId)
            {
                ContentFolder = store.GetItemFolder(properties.ItemId),
                ChangeNote = string.IsNullOrWhiteSpace(changeNotes.InputText) ? null : changeNotes.InputText.Trim()
            };
            if (updateProperties)
                update = update.WithProperties(properties, localDescription, differences);

            // Keeps publish_data.txt in step with the Workshop tools, which list items by it
            store.Save(new PublishData(properties.ItemId, update.Title ?? remote.Title, request.AddonName, DateTimeOffset.Now));

            setLoading(true, "Uploading to Workshop...");
            var progress = new Progress<WorkshopUploadProgress>(p =>
                setLoading(true, p.Fraction is double fraction ? $"{p.Stage}... {fraction:P0}" : $"{p.Stage}..."));
            var result = await client.SubmitUpdateAsync(update, progress);
            setLoading(false, "");

            logger?.Log($"Uploaded {request.AddonName} to Workshop item {properties.ItemId}: {WorkshopLinks.GetItemUrl(properties.ItemId)}", LogType.Success);

            if (result.NeedsLegalAgreement)
            {
                await dialogService.ShowWarningPopup("Workshop Legal Agreement",
                    $"The upload finished, but it stays hidden until you accept the Steam Workshop legal agreement:\n{LegalAgreementUrl}");
            }

            return WorkshopUploadResult.Uploaded;
        }
        catch (WorkshopException ex)
        {
            setLoading(false, "");
            logger?.LogError(ex.Message);
            await dialogService.ShowWarningPopup("Upload Failed", ex.Message);
            return WorkshopUploadResult.Failed;
        }
        catch (VpkPackException ex)
        {
            setLoading(false, "");
            logger?.LogError($"{ex.Message}\n{ex.Output}");
            await dialogService.ShowWarningPopup("Packing Failed", ex.Message);
            return WorkshopUploadResult.Failed;
        }
        catch (Exception ex)
        {
            setLoading(false, "");
            logger?.LogError("Upload failed.");
            _logException(ex);
            return WorkshopUploadResult.Failed;
        }
    }

    private void Pack(WorkshopUploadRequest request, ulong itemId)
    {
        var files = VpkPacker.GetFiles(request.GamePath, request.RemovalGlobs, request.ExcludedPaths);
        logger?.LogDetail($"Packing {files.Count} file{(files.Count == 1 ? "" : "s")}");
        packer.Pack(files, store.GetVpkPath(itemId));
    }

    private async Task<DialogResult> AskAboutMismatch(WorkshopItemDetails remote, WorkshopFields differences)
    {
        var names = Enum.GetValues<WorkshopFields>()
            .Where(field => field != WorkshopFields.None && differences.HasFlag(field))
            .Select(field => field switch
            {
                WorkshopFields.RequiredItems => "Required items",
                _ => field.ToString()
            });

        var response = await dialogService.ShowWarningPopup(new DialogConfiguration
        {
            Title = "Workshop Properties Don't Match",
            Message = $"These properties of \"{remote.Title}\" are different on the Workshop than in this addon:\n\n" +
                string.Join("\n", names.Select(name => $"• {name}")) +
                "\n\nThey may have been edited on the Workshop page.\n\n" +
                "Upload anyway keeps the Workshop's versions. Update Workshop too replaces them with this addon's. " +
                "Open Properties lets you choose for each one.",
            PrimaryButtonText = "Upload anyway",
            SecondaryButtonText = "Update Workshop too",
            TertiaryButtonText = "Open Properties",
            CancelButtonText = "Cancel",
            Width = 560
        });

        return response.Result;
    }
}

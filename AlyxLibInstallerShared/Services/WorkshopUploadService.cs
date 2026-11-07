using AlyxLib.Logging;
using AlyxLib.Workshop;
using AlyxLibInstallerShared.Services.Dialog;
using AlyxLibInstallerShared.ViewModels;

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
/// Packs an addon and uploads it to its linked Workshop item, or a new one, without changing any files in the addon.
/// </summary>
/// <param name="connect">Opens a Workshop connection, which is closed when the upload ends.</param>
/// <param name="setLoading">Shows or hides the loading overlay with a status message.</param>
/// <param name="openUrl">Opens a web page, e.g. the Workshop legal agreement.</param>
/// <param name="previewConverter">Converts preview images Steam won't take as they are.</param>
public class WorkshopUploadService(
    IDialogService dialogService,
    Func<IWorkshopClient> connect,
    WorkshopItemStore store,
    VpkPacker packer,
    Action<bool, string> setLoading,
    ILogger? logger = null,
    Action<Exception>? logException = null,
    Action<string>? openUrl = null,
    IPreviewImageConverter? previewConverter = null)
{
    public const string LegalAgreementUrl = "https://steamcommunity.com/sharedfiles/workshoplegalagreement";

    private readonly Action<Exception> _logException = logException ?? FileLogger.Log;
    private readonly Action<string> _openUrl = openUrl ?? Launcher.Launch;
    private readonly WorkshopLinkService _links = new(dialogService, store, logger);

    /// <summary>
    /// Uploads the addon to its linked Workshop item, asking for change notes first.
    /// If the item's properties on the Workshop don't match the local ones, the user chooses whether to update them too.
    /// An addon without an item gets a new one, after the user fills in its properties.
    /// </summary>
    public async Task<WorkshopUploadResult> UploadAsync(WorkshopUploadRequest request)
    {
        var properties = await _links.LoadOrLinkAsync(request.AddonName, request.ContentPath);
        if (properties == null)
            return WorkshopUploadResult.Cancelled;

        if (!properties.HasItem)
            return await CreateAndUploadAsync(request, properties);

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
                // Linking another item or creating one needs its own connection
                client.Dispose();
                return await RelinkMissingItemAsync(request, properties);
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

            await OfferWorkshopInitRenameAsync(request, properties.ItemId);

            var files = GetFiles(request);
            var changeNote = await AskForChangeNotesAsync(remote.Title, "Upload", files);
            if (changeNote == null)
                return WorkshopUploadResult.Cancelled;

            setLoading(true, "Packing addon...");
            await Task.Run(() => Pack(files, properties.ItemId));

            var update = new WorkshopItemUpdate(properties.ItemId)
            {
                ContentFolder = store.GetItemFolder(properties.ItemId),
                ChangeNote = changeNote,
                // e.g. an item created here whose first upload failed
                PreviewFile = string.IsNullOrEmpty(remote.PreviewUrl) && properties.PreviewFile != null
                    ? PreviewImages.PrepareForUpload(WorkshopProperties.ResolvePath(request.ContentPath, properties.PreviewFile), previewConverter)
                    : null
            };
            if (updateProperties)
                update = update.WithProperties(properties, localDescription, differences);

            // Keeps publish_data.txt in step with the Workshop tools, which list items by it
            store.Save(new PublishData(properties.ItemId, update.Title ?? remote.Title, request.AddonName, DateTimeOffset.Now));

            var result = await SubmitAsync(client, update);
            logger?.Log($"Uploaded {request.AddonName} to Workshop item {properties.ItemId}: {WorkshopLinks.GetItemUrl(properties.ItemId)}", LogType.Success);

            if (result.NeedsLegalAgreement)
                await ShowLegalAgreementAsync(dialogService, _openUrl, "The upload finished");

            return WorkshopUploadResult.Uploaded;
        }
        catch (Exception ex)
        {
            return await FailAsync(ex);
        }
    }

    /// <summary>
    /// Lets the user link another item or create a new one when the linked item no longer exists, then carries on uploading.
    /// </summary>
    private async Task<WorkshopUploadResult> RelinkMissingItemAsync(WorkshopUploadRequest request, WorkshopProperties properties)
    {
        var response = await dialogService.ShowWarningPopup(new DialogConfiguration
        {
            Title = "Workshop Item Not Found",
            Message = $"Workshop item {properties.ItemId} doesn't exist. It may have been deleted.\n\n" +
                "Link this addon to another item, or create a new item for it?",
            PrimaryButtonText = "Change Linked Item",
            SecondaryButtonText = "New Item",
            CancelButtonText = "Cancel",
            Width = 450
        });

        switch (response.Result)
        {
            case DialogResult.Primary:
                return await _links.ChangeLinkAsync(request.AddonName, request.ContentPath, connect, setLoading, linkedItemExists: false)
                    ? await UploadAsync(request)
                    : WorkshopUploadResult.Cancelled;
            case DialogResult.Secondary:
                // The other properties carry over to the new item
                properties.ItemId = 0;
                return await CreateAndUploadAsync(request, properties);
            default:
                logger?.LogDetail("Upload cancelled");
                return WorkshopUploadResult.Cancelled;
        }
    }

    /// <summary>
    /// Has the user fill in the new item's properties, then creates the item and uploads the addon to it.
    /// The item is linked as soon as it's created, so if the upload fails it's retried on the same item.
    /// </summary>
    private async Task<WorkshopUploadResult> CreateAndUploadAsync(WorkshopUploadRequest request, WorkshopProperties properties)
    {
        ulong? createdItemId = null;
        try
        {
            var files = GetFiles(request);
            if (files.Count == 0)
            {
                await dialogService.ShowWarningPopup("Nothing to Upload", $"{request.AddonName} has no files to upload. Its game folder is empty, or every file is on the upload removal list.");
                return WorkshopUploadResult.Failed;
            }

            // Checked now so a missing vpk.exe doesn't leave an empty item on the Workshop
            packer.EnsureAvailable();

            var editor = new WorkshopPropertiesEditorViewModel(properties, request.ContentPath, null, null, dialogService);
            if (!await WorkshopPropertiesService.ShowEditorAsync(dialogService, editor, "New Workshop Item", "Continue"))
            {
                logger?.LogDetail("Upload cancelled");
                return WorkshopUploadResult.Cancelled;
            }

            // Saved now so they don't need entering again if the upload is cancelled or fails
            properties = editor.Build();
            properties.Save(request.ContentPath);
            var description = editor.CurrentDescription;

            // Starts with the Workshop tools' note for a new item
            var changeNote = await AskForChangeNotesAsync(properties.Title!, "Create and Upload", files, $"Created {properties.Title}.");
            if (changeNote == null)
                return WorkshopUploadResult.Cancelled;

            var preview = PreviewImages.PrepareForUpload(WorkshopProperties.ResolvePath(request.ContentPath, properties.PreviewFile!), previewConverter);

            setLoading(true, "Connecting to Steam...");
            using var client = connect();

            setLoading(true, "Creating Workshop item...");
            var created = await client.CreateItemAsync();
            createdItemId = created.ItemId;
            properties.ItemId = created.ItemId;
            properties.Save(request.ContentPath);
            logger?.LogInfo($"Created Workshop item {created.ItemId} for {request.AddonName}");

            setLoading(false, "");
            if (await OfferWorkshopInitRenameAsync(request, created.ItemId))
                files = GetFiles(request);

            setLoading(true, "Packing addon...");
            await Task.Run(() => Pack(files, created.ItemId));
            store.Save(new PublishData(created.ItemId, properties.Title!, request.AddonName, DateTimeOffset.Now));

            var update = new WorkshopItemUpdate(created.ItemId)
            {
                ContentFolder = store.GetItemFolder(created.ItemId),
                ChangeNote = changeNote,
                PreviewFile = preview
            }.WithAllProperties(properties, description);

            var result = await SubmitAsync(client, update);
            logger?.Log($"Uploaded {request.AddonName} to new Workshop item {created.ItemId}: {WorkshopLinks.GetItemUrl(created.ItemId)}", LogType.Success);

            if (created.NeedsLegalAgreement || result.NeedsLegalAgreement)
                await ShowLegalAgreementAsync(dialogService, _openUrl, "The upload finished");

            return WorkshopUploadResult.Uploaded;
        }
        catch (Exception ex)
        {
            var retry = createdItemId is ulong itemId
                ? $"\n\nWorkshop item {itemId} was created and linked to this addon. Use Upload again to finish uploading to it."
                : "";
            return await FailAsync(ex, retry);
        }
    }

    /// <param name="defaultNote">Text the change notes start with.</param>
    /// <returns>The trimmed change notes, "" if there are none, or null if the user cancelled.</returns>
    private async Task<string?> AskForChangeNotesAsync(string title, string uploadText, IReadOnlyList<PackFile> files, string? defaultNote = null)
    {
        var response = await dialogService.ShowTextPopup(new DialogConfiguration
        {
            Title = $"Upload {title}",
            Message = $"Uploading {DescribeFiles(files)}.\n\nDescribe what changed in this update. Change notes are shown on the item's Workshop page.",
            HasTextBox = true,
            TextBoxIsMultiline = true,
            TextBoxPlaceholderText = "Change notes (optional)",
            TextBoxDefaultText = defaultNote,
            PrimaryButtonText = uploadText,
            CancelButtonText = "Cancel",
            Width = 450
        });
        if (response.Result != DialogResult.Primary)
        {
            logger?.LogDetail("Upload cancelled");
            return null;
        }

        return response.InputText?.Trim() ?? "";
    }

    private async Task<WorkshopSubmitResult> SubmitAsync(IWorkshopClient client, WorkshopItemUpdate update)
    {
        // Steam shows empty change notes as blank entries
        if (string.IsNullOrEmpty(update.ChangeNote))
            update = update with { ChangeNote = null };

        setLoading(true, "Uploading to Workshop...");
        var progress = new Progress<WorkshopUploadProgress>(p =>
            setLoading(true, p.Fraction is double fraction ? $"{p.Stage}... {fraction:P0}" : $"{p.Stage}..."));
        var result = await client.SubmitUpdateAsync(update, progress);
        setLoading(false, "");
        return result;
    }

    /// <summary>
    /// Tells the user their item stays hidden until they accept the Workshop legal agreement, and offers to open it.
    /// </summary>
    /// <param name="whatFinished">What succeeded, e.g. "The upload finished".</param>
    internal static async Task ShowLegalAgreementAsync(IDialogService dialogService, Action<string> openUrl, string whatFinished)
    {
        var response = await dialogService.ShowWarningPopup(new DialogConfiguration
        {
            Title = "Workshop Legal Agreement",
            Message = $"{whatFinished}, but the item stays hidden from other players until you accept the Steam Workshop legal agreement.\n\n{LegalAgreementUrl}",
            PrimaryButtonText = "Open Agreement",
            CancelButtonText = "Later",
            Width = 450
        });

        if (response.Result == DialogResult.Primary)
            openUrl(LegalAgreementUrl);
    }

    /// <param name="note">Added to the message shown to the user.</param>
    private async Task<WorkshopUploadResult> FailAsync(Exception ex, string note = "")
    {
        setLoading(false, "");
        switch (ex)
        {
            case WorkshopException:
                logger?.LogError(ex.Message);
                await dialogService.ShowWarningPopup("Upload Failed", ex.Message + note);
                break;
            case VpkPackException pack:
                logger?.LogError($"{pack.Message}\n{pack.Output}");
                await dialogService.ShowWarningPopup("Packing Failed", pack.Message + note);
                break;
            default:
                logger?.LogError("Upload failed." + note);
                _logException(ex);
                break;
        }
        return WorkshopUploadResult.Failed;
    }

    /// <summary>
    /// Offers to rename AlyxLib's Workshop init script to the item's ID, which is how it knows the addon is running from the Workshop.
    /// It's renamed in the content folder and, if it isn't linked there, the game folder, so the upload includes it.
    /// </summary>
    /// <returns>True if it was renamed.</returns>
    private async Task<bool> OfferWorkshopInitRenameAsync(WorkshopUploadRequest request, ulong itemId)
    {
        var placeholders = new[] { request.ContentPath, request.GamePath }
            .Select(root => Path.Combine(root, WorkshopInitFolder, WorkshopInitPlaceholder))
            .Where(File.Exists)
            .ToList();
        var renamed = $"{itemId}.lua";
        if (placeholders.Count == 0 || placeholders.Any(path => File.Exists(Path.Combine(Path.GetDirectoryName(path)!, renamed))))
            return false;

        var response = await dialogService.ShowTextPopup(new DialogConfiguration
        {
            Title = "Rename Workshop Init Script",
            Message = $"This addon has AlyxLib's Workshop init script, {WorkshopInitFolder}/{WorkshopInitPlaceholder}.\n\n" +
                $"It needs renaming to {renamed}, this item's ID, to run for players who subscribe. Rename it before uploading?",
            PrimaryButtonText = "Rename",
            CancelButtonText = "Not now",
            Width = 450
        });
        if (response.Result != DialogResult.Primary)
            return false;

        // A linked scripts folder means the game folder's copy is already renamed
        foreach (var path in placeholders.Where(File.Exists))
            File.Move(path, Path.Combine(Path.GetDirectoryName(path)!, renamed));
        logger?.LogInfo($"Renamed {WorkshopInitPlaceholder} to {renamed}");
        return true;
    }

    private const string WorkshopInitFolder = "scripts/vscripts/mods/init";
    private const string WorkshopInitPlaceholder = "0000000000.lua";

    private static string DescribeFiles(IReadOnlyList<PackFile> files)
    {
        var size = files.Sum(file => new FileInfo(file.SourcePath).Length);
        return $"{files.Count} file{(files.Count == 1 ? "" : "s")}, {Utils.FormatFileSize(size)}";
    }

    private IReadOnlyList<PackFile> GetFiles(WorkshopUploadRequest request) =>
        VpkPacker.GetFiles(request.GamePath, request.RemovalGlobs, request.ExcludedPaths);

    private void Pack(IReadOnlyList<PackFile> files, ulong itemId)
    {
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

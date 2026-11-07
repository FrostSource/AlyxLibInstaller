using AlyxLib.Logging;
using AlyxLib.Workshop;
using AlyxLibInstallerShared.Services.Dialog;
using AlyxLibInstallerShared.ViewModels;

namespace AlyxLibInstallerShared.Services;

/// <summary>
/// Links addons to the Workshop items they're uploaded to.
/// </summary>
public class WorkshopLinkService(IDialogService dialogService, WorkshopItemStore store, ILogger? logger = null)
{
    /// <summary>
    /// Loads the addon's Workshop properties, or links it to the item it was last uploaded as with the Workshop tools if the user agrees.
    /// </summary>
    /// <returns>The properties, or null if there's nothing to link or the user cancelled.</returns>
    public async Task<WorkshopProperties?> LoadOrLinkAsync(string addonName, string contentPath)
    {
        var existing = WorkshopProperties.Load(contentPath);
        if (existing != null)
            return existing;

        var items = store.GetItemsForAddon(addonName);
        if (items.Count == 0)
        {
            await dialogService.ShowTextPopup(new DialogConfiguration
            {
                Title = "No Workshop Item",
                Message = $"{addonName} hasn't been uploaded to the Workshop yet. Upload it once with the Workshop Tools, then use Upload here for updates.",
                Width = 400
            });
            return null;
        }

        var item = items[0];
        var others = items.Count > 1
            ? $"\n\nIt has also been uploaded as:\n{string.Join("\n", items.Skip(1).Select(other => $"• {other.Title} ({other.ItemId})"))}\n\nUse Change Linked Item to pick one of those instead."
            : "";

        var response = await dialogService.ShowTextPopup(new DialogConfiguration
        {
            Title = "Link Workshop Item",
            Message = $"{addonName} was last uploaded as \"{item.Title}\" ({item.ItemId}).{others}\n\nLink it to this item so Upload sends updates to it?",
            PrimaryButtonText = "Link item",
            CancelButtonText = "Cancel",
            Width = 450
        });
        if (response.Result != DialogResult.Primary)
            return null;

        var properties = new WorkshopProperties { ItemId = item.ItemId };
        properties.Save(contentPath);
        logger?.LogInfo($"Linked {addonName} to Workshop item {item.ItemId}");
        return properties;
    }

    /// <summary>
    /// Lets the user link the addon to another of its published items, or any item by ID or link.
    /// The new item is checked on Steam before it's linked. Other saved properties are kept.
    /// </summary>
    /// <returns>True if the link changed.</returns>
    public async Task<bool> ChangeLinkAsync(string addonName, string contentPath, Func<IWorkshopClient> connect, Action<bool, string> setLoading)
    {
        var properties = WorkshopProperties.Load(contentPath);
        var picker = new ChangeLinkedItemViewModel(store.GetItemsForAddon(addonName), properties?.ItemId);

        while (true)
        {
            var response = await dialogService.ShowChangeLinkedItemPopup(new DialogConfiguration
            {
                Title = "Change Linked Item",
                PrimaryButtonText = "Link",
                CancelButtonText = "Cancel",
                Width = 450
            }, picker);
            if (response.Result != DialogResult.Primary)
                return false;

            if (picker.SelectedItemId is not ulong itemId)
            {
                await dialogService.ShowWarningPopup("No Item Chosen", "Choose an item, or enter a Workshop item ID or link.");
                continue;
            }

            if (itemId == properties?.ItemId)
                return false;

            string? problem;
            try
            {
                setLoading(true, "Checking Workshop item...");
                using var client = connect();
                problem = await client.GetItemAsync(itemId) == null ? $"Workshop item {itemId} doesn't exist." : null;
            }
            catch (WorkshopException ex)
            {
                problem = ex.Message;
            }
            finally
            {
                setLoading(false, "");
            }

            if (problem != null)
            {
                await dialogService.ShowWarningPopup("Can't Link Item", problem);
                continue;
            }

            properties ??= new WorkshopProperties();
            properties.ItemId = itemId;
            properties.Save(contentPath);
            logger?.LogInfo($"Linked {addonName} to Workshop item {itemId}");
            return true;
        }
    }
}

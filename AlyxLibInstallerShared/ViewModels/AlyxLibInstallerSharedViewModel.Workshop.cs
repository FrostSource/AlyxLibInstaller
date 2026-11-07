using AlyxLib.Workshop;
using AlyxLibInstallerShared.Services;
using CommunityToolkit.Mvvm.Input;

namespace AlyxLibInstallerShared.ViewModels;

public partial class AlyxLibInstallerSharedViewModel
{
    [RelayCommand]
    private async Task UploadToWorkshop()
    {
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot upload");
            return;
        }

        IReadOnlyList<string> excludedPaths;
        try
        {
            excludedPaths = AlyxLibInstance.FileManager.GetUploadExcludedPaths(SelectedAddon, AddonConfig.GetConfig());
        }
        catch (InvalidOperationException ex)
        {
            // Packing without these could upload AlyxLib inside the addon
            _logger?.LogError($"{ex.Message} Cannot upload.");
            return;
        }

        var uploadService = new WorkshopUploadService(
            _dialogService,
            SteamWorkshopClient.Connect,
            WorkshopItemStore.ForInstalledGame(),
            VpkPacker.ForInstalledGame(),
            SetLoading,
            _logger);

        var request = new WorkshopUploadRequest(
            SelectedAddon.Name,
            SelectedAddon.ContentPath,
            SelectedAddon.GamePath,
            AddonConfig.FileRemovalGlobs,
            excludedPaths);

        // After sorting out mismatched properties, the upload carries on with them
        while (await uploadService.UploadAsync(request) == WorkshopUploadResult.OpenProperties)
        {
            if (!await CreatePropertiesService().EditAsync(SelectedAddon.Name, SelectedAddon.ContentPath))
                return;
        }
    }

    [RelayCommand]
    private async Task ShowWorkshopProperties()
    {
        if (SelectedAddon == null)
        {
            _logger?.LogError("No addon selected, cannot edit Workshop properties");
            return;
        }

        await CreatePropertiesService().EditAsync(SelectedAddon.Name, SelectedAddon.ContentPath);
    }

    [RelayCommand]
    private async Task ChangeLinkedWorkshopItem()
    {
        if (SelectedAddon == null)
        {
            _logger?.LogError("No addon selected, cannot change linked Workshop item");
            return;
        }

        var links = new WorkshopLinkService(_dialogService, WorkshopItemStore.ForInstalledGame(), _logger);
        await links.ChangeLinkAsync(SelectedAddon.Name, SelectedAddon.ContentPath, SteamWorkshopClient.Connect, SetLoading);
    }

    private WorkshopPropertiesService CreatePropertiesService() =>
        new(_dialogService, SteamWorkshopClient.Connect, WorkshopItemStore.ForInstalledGame(), SetLoading, _logger);

    [RelayCommand]
    private void OpenWorkshopPage()
    {
        if (SelectedAddon == null)
        {
            _logger?.LogError("No addon selected, cannot open Workshop page");
            return;
        }

        var properties = WorkshopProperties.Load(SelectedAddon.ContentPath);
        if (properties == null)
        {
            _logger?.LogWarning($"{SelectedAddon.Name} isn't linked to a Workshop item yet. Upload it to link it.");
            return;
        }

        Launcher.Launch(WorkshopLinks.GetItemUrl(properties.ItemId));
    }
}

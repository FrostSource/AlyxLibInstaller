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

        await uploadService.UploadAsync(new WorkshopUploadRequest(
            SelectedAddon.Name,
            SelectedAddon.ContentPath,
            SelectedAddon.GamePath,
            AddonConfig.FileRemovalGlobs,
            excludedPaths));
    }

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

        Launcher.Launch(WorkshopUploadService.GetItemUrl(properties.ItemId));
    }
}

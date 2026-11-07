using AlyxLib.Workshop;

namespace AlyxLibInstallerShared.Services;

/// <summary>
/// Converts preview images to JPEG, which the platform's imaging does.
/// </summary>
public interface IPreviewImageConverter
{
    /// <summary>
    /// Saves the image as a JPEG under the size limit, scaling it down if lowering the quality isn't enough.
    /// </summary>
    void ConvertToJpeg(string sourcePath, string destinationPath, long maxBytes);
}

/// <summary>
/// Gets preview images ready for Steam, which only takes PNG, JPG and GIF images up to 1 MB.
/// Others are converted to JPEG when they're uploaded, like the Workshop tools do.
/// </summary>
public static class PreviewImages
{
    public const long MaxBytes = 1024 * 1024;

    private static readonly string[] SteamExtensions = [".png", ".jpg", ".jpeg", ".gif"];

    public static bool NeedsConverting(string path) =>
        new FileInfo(path).Length > MaxBytes
        || !SteamExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the image to upload: the original, or a converted copy in a temporary folder.
    /// </summary>
    /// <exception cref="WorkshopException">The image needs converting and there's no converter, or it couldn't be converted.</exception>
    public static string PrepareForUpload(string path, IPreviewImageConverter? converter)
    {
        if (!NeedsConverting(path))
            return path;

        if (converter == null)
            throw new WorkshopException($"The preview image must be a PNG, JPG or GIF up to {Utils.FormatFileSize(MaxBytes)}: {path}");

        var converted = Path.Combine(Path.GetTempPath(), "AlyxLibInstaller", "preview.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(converted)!);
        try
        {
            converter.ConvertToJpeg(path, converted, MaxBytes);
        }
        catch (Exception ex) when (ex is not WorkshopException)
        {
            throw new WorkshopException($"The preview image couldn't be converted to JPEG: {ex.Message}");
        }
        return converted;
    }
}
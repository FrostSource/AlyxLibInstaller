using AlyxLibInstallerShared.Services;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AlyxLibInstallerWPF.Services;

/// <summary>
/// Converts preview images to JPEG with WPF's imaging.
/// </summary>
public class WpfPreviewImageConverter : IPreviewImageConverter
{
    public void ConvertToJpeg(string sourcePath, string destinationPath, long maxBytes)
    {
        BitmapSource image;
        using (var stream = File.OpenRead(sourcePath))
            image = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];

        // JPEG has no transparency
        image = new FormatConvertedBitmap(image, PixelFormats.Bgr24, null, 0);

        // Lower the quality first, then scale down until it fits
        for (var scale = 1.0; ; scale *= 0.8)
        {
            var scaled = scale == 1.0 ? image : new TransformedBitmap(image, new ScaleTransform(scale, scale));
            foreach (var quality in new[] { 92, 85, 75 })
            {
                var bytes = Encode(scaled, quality);
                if (bytes.Length <= maxBytes || scaled.PixelWidth <= 64)
                {
                    File.WriteAllBytes(destinationPath, bytes);
                    return;
                }
            }
        }
    }

    private static byte[] Encode(BitmapSource image, int quality)
    {
        var encoder = new JpegBitmapEncoder { QualityLevel = quality };
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace AlyxLibInstallerWPF;

/// <summary>
/// Shows an image file or URL in an <see cref="Image"/>, playing animated GIFs, which WPF only shows the first frame of.
/// Files are read into memory so they aren't locked while shown.
/// </summary>
public static class AnimatedImage
{
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.RegisterAttached("Source", typeof(string), typeof(AnimatedImage), new PropertyMetadata(null, OnSourceChanged));

    public static string? GetSource(Image image) => (string?)image.GetValue(SourceProperty);
    public static void SetSource(Image image, string? value) => image.SetValue(SourceProperty, value);

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var image = (Image)d;
        image.BeginAnimation(Image.SourceProperty, null);
        image.Source = null;

        var source = e.NewValue as string;
        if (string.IsNullOrEmpty(source))
            return;

        try
        {
            if (!File.Exists(source))
            {
                // e.g. the Workshop's current preview
                image.Source = new BitmapImage(new Uri(source));
                return;
            }

            var bytes = File.ReadAllBytes(source);
            var animation = Path.GetExtension(source).Equals(".gif", StringComparison.OrdinalIgnoreCase) ? CreateGifAnimation(bytes) : null;
            if (animation != null)
                image.BeginAnimation(Image.SourceProperty, animation);
            else
                image.Source = Load(bytes);
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UriFormatException or ArgumentException or FileFormatException)
        {
            // A broken image just isn't shown, the editor reports missing files
        }
    }

    private static BitmapImage Load(byte[] bytes)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// Builds each frame of the GIF onto the full image, following its frame delays and disposal methods.
    /// </summary>
    /// <returns>The animation, or null if the GIF has only one frame.</returns>
    private static ObjectAnimationUsingKeyFrames? CreateGifAnimation(byte[] bytes)
    {
        var decoder = new GifBitmapDecoder(new MemoryStream(bytes), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (decoder.Frames.Count < 2)
            return null;

        int width = GetMetadata<ushort>(decoder.Metadata, "/logscrdesc/Width") is ushort w and > 0 ? w : decoder.Frames[0].PixelWidth;
        int height = GetMetadata<ushort>(decoder.Metadata, "/logscrdesc/Height") is ushort h and > 0 ? h : decoder.Frames[0].PixelHeight;
        var full = new Rect(0, 0, width, height);

        var animation = new ObjectAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
        var time = TimeSpan.Zero;
        BitmapSource? background = null;

        foreach (var frame in decoder.Frames)
        {
            var metadata = (BitmapMetadata)frame.Metadata;
            var area = new Rect(
                GetMetadata<ushort>(metadata, "/imgdesc/Left") ?? 0,
                GetMetadata<ushort>(metadata, "/imgdesc/Top") ?? 0,
                frame.PixelWidth,
                frame.PixelHeight);

            var composed = Render(width, height, context =>
            {
                if (background != null)
                    context.DrawImage(background, full);
                context.DrawImage(frame, area);
            });
            animation.KeyFrames.Add(new DiscreteObjectKeyFrame(composed, KeyTime.FromTimeSpan(time)));

            // Delays are in hundredths of a second. Browsers treat very short ones as 100 ms, so do the same
            var delay = GetMetadata<ushort>(metadata, "/grctlext/Delay") ?? 0;
            time += TimeSpan.FromMilliseconds(delay < 2 ? 100 : delay * 10);

            switch (GetMetadata<byte>(metadata, "/grctlext/Disposal") ?? 0)
            {
                case 2:
                    // Restore to background: clear this frame's area
                    var cleared = composed;
                    background = Render(width, height, context =>
                    {
                        context.PushClip(new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(full), new RectangleGeometry(area)));
                        context.DrawImage(cleared, full);
                        context.Pop();
                    });
                    break;
                case 3:
                    // Restore to previous: the next frame draws on the same background as this one
                    break;
                default:
                    background = composed;
                    break;
            }
        }

        animation.Duration = time;
        animation.Freeze();
        return animation;
    }

    private static RenderTargetBitmap Render(int width, int height, Action<DrawingContext> draw)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
            draw(context);
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static T? GetMetadata<T>(BitmapMetadata? metadata, string query) where T : struct
    {
        try
        {
            return metadata?.GetQuery(query) is T value ? value : null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
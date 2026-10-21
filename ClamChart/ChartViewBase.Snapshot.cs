namespace ClamChart;

using SkiaSharp;

public abstract partial class ChartViewBase
{
    private Size? snapshotSize;
    private bool snapshotRestoring;

    internal double LayoutWidth => snapshotSize?.Width ?? Width;

    internal double LayoutHeight => snapshotSize?.Height ?? Height;

    internal bool IsSnapshot => snapshotSize is not null;

    public SKImage RenderToImage(Size size, float scale)
    {
        ThrowIfUnavailable();
        var info = new SKImageInfo((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale), SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = ((info.Width > 0) && (info.Height > 0) ? SKSurface.Create(info) : null) ?? throw new ArgumentOutOfRangeException(nameof(size), size, "The size and the scale must make an image of one pixel or more.");
        CancelInput(true);
        FinishAnimations();
        EnsureData();
        var width = (float)size.Width;
        var height = (float)size.Height;
        snapshotSize = size;
        try
        {
            RunLayout(width, height);
            var canvas = surface.Canvas;
            canvas.Clear();
            canvas.Scale(scale);
            if (IsBusy)
            {
                DrawSkeleton(canvas, width, height);
            }
            else
            {
                Render(canvas, width, height);
            }

            return surface.Snapshot();
        }
        finally
        {
            snapshotSize = null;
            snapshotRestoring = true;
            layoutDirty = true;
            InvalidateCanvas();
        }
    }

    public async Task SaveAsPngAsync(Stream stream, Size size, float scale)
    {
        using var image = RenderToImage(size, scale);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var source = data.AsStream();
        await using (source.ConfigureAwait(false))
        {
            await source.CopyToAsync(stream).ConfigureAwait(false);
        }
    }
}

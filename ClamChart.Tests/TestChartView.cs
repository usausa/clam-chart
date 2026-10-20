namespace ClamChart.Tests;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class TestChartView : ChartViewBase
{
    public static readonly BindableProperty DataInputProperty = BindableProperty.Create(nameof(DataInput), typeof(int), typeof(TestChartView), propertyChanged: static (bindable, _, _) => ((TestChartView)bindable).InvalidateData());
    public static readonly BindableProperty LayoutInputProperty = BindableProperty.Create(nameof(LayoutInput), typeof(int), typeof(TestChartView), propertyChanged: OnLayoutInputChanged);

    public int DataInput
    {
        get => (int)GetValue(DataInputProperty);
        set => SetValue(DataInputProperty, value);
    }

    public int LayoutInput
    {
        get => (int)GetValue(LayoutInputProperty);
        set => SetValue(LayoutInputProperty, value);
    }

    public int DataUpdates { get; private set; }

    public int LayoutUpdates { get; private set; }

    public int Renders { get; private set; }

    public int RendererResets { get; private set; }

    public int InputCancels { get; private set; }

    public int EntranceStarts { get; private set; }

    public int AnimationFinishes { get; private set; }

    public int Detaches { get; private set; }

    public float LastLayoutWidth { get; private set; }

    public float LastLayoutHeight { get; private set; }

    public float LastRenderWidth { get; private set; }

    public float LastRenderScale { get; private set; }

    internal override int RenderedPointCount => 42;

    public void ChangeDataInput() => DataInput++;

    public void Paint(double width, double height)
    {
        Frame = new Rect(0, 0, width, height);
        var info = new SKImageInfo((int)Math.Max(1, width * 2), (int)Math.Max(1, height * 2));
        using var surface = SKSurface.Create(info);
        OnPaintSurface(new SKPaintSurfaceEventArgs(surface, info));
    }

    internal override void Render(SKCanvas canvas, float width, float height)
    {
        Renders++;
        LastRenderWidth = width;
        LastRenderScale = canvas.TotalMatrix.ScaleX;
    }

    internal override void UpdateData() => DataUpdates++;

    internal override void UpdateLayout(float width, float height)
    {
        LayoutUpdates++;
        LastLayoutWidth = width;
        LastLayoutHeight = height;
    }

    internal override void ResetRenderer() => RendererResets++;

    internal override void OnInputCanceled() => InputCancels++;

    internal override void StartEntranceAnimation() => EntranceStarts++;

    internal override void FinishAnimations() => AnimationFinishes++;

    internal override void OnDetached() => Detaches++;
}

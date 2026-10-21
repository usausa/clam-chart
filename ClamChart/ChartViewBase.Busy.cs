namespace ClamChart;

using ClamChart.Animation;
using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;

public abstract partial class ChartViewBase
{
    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(nameof(IsBusy), typeof(bool), typeof(ChartViewBase), false, propertyChanged: OnBusyChanged);

    private const string SkeletonAnimationName = "ClamChartSkeleton";

    private const double SkeletonMilliseconds = 1600;

    private ChartAnimator? skeletonAnimator;
    private SkeletonPainter? skeletonPainter;

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    internal virtual SkeletonShape SkeletonShape => SkeletonShape.Line;

    internal bool IsSkeletonAnimating => skeletonAnimator is { IsRunning: true };

    internal double SkeletonPhase => skeletonAnimator is { IsRunning: true } animator ? animator.Progress : Double.NaN;

    internal void StopSkeleton() => skeletonAnimator?.Stop();

    private static void OnBusyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ChartViewBase)bindable;
        if (!view.BeginChange())
        {
            return;
        }

        if (view.IsBusy)
        {
            view.CancelInput(true);
            view.StartSkeleton();
        }
        else
        {
            view.StopSkeleton();
            view.StartEntranceAnimation();
        }

        view.InvalidateCanvas();
    }

    private void StartSkeleton()
    {
        if (IsSkeletonAnimating || windowStopped)
        {
            return;
        }

        var length = ResolveAnimationLength(SkeletonMilliseconds);
        if (length < 1)
        {
            return;
        }

        skeletonAnimator ??= new ChartAnimator(this, SkeletonAnimationName, OnSkeletonProgress);
        skeletonAnimator.Start(length, true);
    }

    private void OnSkeletonProgress(double progress) => InvalidateCanvas();

    private void DrawSkeleton(SKCanvas canvas, float width, float height)
    {
        StartSkeleton();
        var padding = EffectiveContentPadding;
        var bounds = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        skeletonPainter ??= new SkeletonPainter();
        skeletonPainter.Draw(canvas, bounds, SkeletonShape, ChartStyle, IsSnapshot ? Double.NaN : SkeletonPhase);
    }

    private void ReleaseSkeleton()
    {
        StopSkeleton();
        skeletonPainter?.Dispose();
        skeletonPainter = null;
    }
}

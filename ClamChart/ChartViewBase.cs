namespace ClamChart;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

using ClamChart.Animation;
using ClamChart.Layout;

using Microsoft.Maui.Converters;

using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

public abstract partial class ChartViewBase : SKCanvasView, IDisposable
{
    public static readonly BindableProperty DensityProperty = BindableProperty.Create(nameof(Density), typeof(ChartDensity), typeof(ChartViewBase), ChartDensity.Auto, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty PaletteProperty = BindableProperty.Create(nameof(Palette), typeof(IReadOnlyList<Color>), typeof(ChartViewBase), ChartPalettes.DefaultLight, propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(nameof(ContentPadding), typeof(Thickness?), typeof(ChartViewBase), validateValue: static (_, value) => ValidatePadding(value, nameof(ContentPadding)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(ChartViewBase), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(nameof(EmptyText), typeof(string), typeof(ChartViewBase), propertyChanged: OnRenderInputChanged);

    private readonly Action processUpdate;
    private bool dataDirty = true;
    private bool layoutDirty = true;
    private bool updateScheduled;
    private bool disposed;
    private Window? watchedWindow;
    private bool windowStopped;

    public event EventHandler<ChartFrameEventArgs>? FrameRendered;

    public ChartDensity Density
    {
        get => (ChartDensity)GetValue(DensityProperty);
        set => SetValue(DensityProperty, value);
    }

    public ChartDensity EffectiveDensity => ResolveDensity();

    public IReadOnlyList<Color> Palette
    {
        get => (IReadOnlyList<Color>)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    [TypeConverter(typeof(ThicknessTypeConverter))]
    public Thickness? ContentPadding
    {
        get => (Thickness?)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public CultureInfo? Culture
    {
        get => (CultureInfo?)GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    public string? EmptyText
    {
        get => (string?)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    internal Thickness EffectiveContentPadding => ContentPadding ?? GetDensityPadding(EffectiveDensity);

    internal virtual int RenderedPointCount => 0;

    internal virtual ChartDensity ResolveDensity() =>
        Density == ChartDensity.Auto ? DensityResolver.Resolve(LayoutWidth, LayoutHeight, EffectiveStyle.CompactSize, EffectiveStyle.MinimalSize) : Density;

    internal ChartStyle EffectiveStyle
    {
        get => field ??= BuildStyle();
        private set;
    }

    internal static Thickness GetDensityPadding(ChartDensity density) =>
        new(density switch
        {
            ChartDensity.Full => 8,
            ChartDensity.Compact => 4,
            _ => 0
        });

    internal ChartViewBase()
    {
        processUpdate = ProcessUpdate;
        AnimationDriver = new MauiAnimationDriver(this);
        EnableTouchEvents = true;
        Touch += OnChartTouch;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
    }

    //--------------------------------------------------------------------------------
    // Lifecycle
    //--------------------------------------------------------------------------------

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || disposed)
        {
            return;
        }

        disposed = true;
        CancelInput(true);
        FinishAnimations();
        ReleaseSkeleton();
        StopEffect();
        OnDetached();
        ResetRenderer();
        ReleaseInput();
        Touch -= OnChartTouch;
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
        WatchWindow(null);
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        CancelInput(true);
        if (args.NewHandler is null)
        {
            FinishAnimations();
        }

        ReleaseSkeleton();
        StopEffect();
        OnDetached();
        DetachPlatform();
        ResetRenderer();
        layoutDirty = true;
        base.OnHandlerChanging(args);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (!disposed && (Handler is not null))
        {
            OnAttached();
            dataDirty = true;
            layoutDirty = true;
            ScheduleUpdate();
        }

        AttachPlatform();
        InvalidateCanvas();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if ((propertyName is nameof(IsEnabled) or nameof(IsVisible)) && (!IsEnabled || !IsVisible))
        {
            CancelInput(true);
        }

        if ((propertyName == nameof(IsVisible)) && !IsVisible)
        {
            FinishAnimations();
            StopSkeleton();
            StopEffect();
        }

        if ((propertyName == nameof(Window)) && !disposed)
        {
            WatchWindow(Window);
        }
    }

    public void Invalidate()
    {
        ThrowIfUnavailable();
        EffectiveStyle = null!;
        ResetRenderer();
        layoutDirty = true;
        InvalidateCanvas();
    }

    //--------------------------------------------------------------------------------
    // Render
    //--------------------------------------------------------------------------------

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();
        if (disposed || (Width <= 0) || (Height <= 0))
        {
            return;
        }

        var started = Stopwatch.GetTimestamp();
        if (!updateScheduled)
        {
            EnsureData();
        }

        var width = (float)Width;
        var height = (float)Height;
        if (layoutDirty)
        {
            RunLayout(width, height);
        }

        canvas.Save();
        try
        {
            canvas.Scale((float)(e.Info.Width / Width), (float)(e.Info.Height / Height));
            if (IsBusy)
            {
                DrawSkeleton(canvas, width, height);
            }
            else
            {
                Render(canvas, width, height);
                DrawPressedOverlay(canvas, width, height);
            }
        }
        finally
        {
            canvas.Restore();
        }

        UpdateEffect();
        base.OnPaintSurface(e);
        FrameRendered?.Invoke(this, new ChartFrameEventArgs(Stopwatch.GetElapsedTime(started).TotalMilliseconds, RenderedPointCount, IsAnimating || IsSkeletonAnimating || IsEffectAnimating));
    }

    internal abstract void Render(SKCanvas canvas, float width, float height);

    internal virtual void UpdateData()
    {
    }

    internal virtual void UpdateLayout(float width, float height)
    {
    }

    internal virtual void ResetRenderer()
    {
    }

    internal virtual void OnAttached()
    {
    }

    internal virtual void OnDetached()
    {
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    internal static void OnLayoutInputChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartViewBase)bindable).InvalidateLayout();

    internal static void OnRenderInputChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartViewBase)bindable).InvalidateRender();

    internal static void OnStyleChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartViewBase)bindable).InvalidateStyle();

    internal static void OnStyleRenderChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ChartViewBase)bindable;
        view.EffectiveStyle = null!;
        view.InvalidateRender();
    }

    internal void InvalidateData()
    {
        if (!BeginChange())
        {
            return;
        }

        dataDirty = true;
        layoutDirty = true;
        ScheduleUpdate();
    }

    internal void InvalidateLayout()
    {
        if (!BeginChange())
        {
            return;
        }

        layoutDirty = true;
        InvalidateCanvas();
    }

    internal void InvalidateRender()
    {
        if (BeginChange())
        {
            InvalidateCanvas();
        }
    }

    internal void InvalidateSize()
    {
        InvalidateMeasure();
        InvalidateLayout();
    }

    internal void InvalidateCanvas()
    {
        if (Handler?.PlatformView is not null)
        {
            InvalidateSurface();
        }
    }

    internal void InvalidateStyle()
    {
        EffectiveStyle = null!;
        if (!BeginChange())
        {
            return;
        }

        ResetRenderer();
        layoutDirty = true;
        InvalidateCanvas();
    }

    private bool BeginChange()
    {
        if (disposed)
        {
            return false;
        }

        RequireUiThread();
        return true;
    }

    private void ScheduleUpdate()
    {
        if (updateScheduled)
        {
            return;
        }

        if (FindDispatcher() is { } dispatcher)
        {
            updateScheduled = true;
            dispatcher.Dispatch(processUpdate);
        }
        else
        {
            ProcessUpdate();
        }
    }

    private void ProcessUpdate()
    {
        updateScheduled = false;
        if (!disposed && dataDirty)
        {
            EnsureData();
            InvalidateCanvas();
        }
    }

    internal void EnsureLayout()
    {
        if (!updateScheduled)
        {
            EnsureData();
        }

        if (layoutDirty && (Width > 0) && (Height > 0))
        {
            RunLayout((float)Width, (float)Height);
        }
    }

    private void RunLayout(float width, float height)
    {
        layoutDirty = false;
        UpdateLayout(width, height);
        UpdateAutomationSummary();
        if (!IsSnapshot)
        {
            snapshotRestoring = false;
        }
    }

    private void EnsureData()
    {
        if (dataDirty)
        {
            dataDirty = false;
            layoutDirty = true;
            UpdateData();
        }
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        CancelInput();
        layoutDirty = true;
        InvalidateCanvas();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        CancelInput(true);
        FinishAnimations();
        StopSkeleton();
        StopEffect();
    }

    private void WatchWindow(Window? value)
    {
        if (ReferenceEquals(watchedWindow, value))
        {
            return;
        }

        if (watchedWindow is not null)
        {
            watchedWindow.Stopped -= OnWindowStopped;
            watchedWindow.Resumed -= OnWindowResumed;
        }

        watchedWindow = value;
        windowStopped = false;
        if (watchedWindow is not null)
        {
            watchedWindow.Stopped += OnWindowStopped;
            watchedWindow.Resumed += OnWindowResumed;
        }
    }

    private void OnWindowStopped(object? sender, EventArgs e)
    {
        windowStopped = true;
        StopSkeleton();
        StopEffect();
    }

    private void OnWindowResumed(object? sender, EventArgs e)
    {
        windowStopped = false;
        InvalidateCanvas();
    }

    //--------------------------------------------------------------------------------
    // Thread
    //--------------------------------------------------------------------------------

    internal void RequireUiThread()
    {
        if (FindDispatcher() is { IsDispatchRequired: true })
        {
            throw new InvalidOperationException("Chart changes must run on the UI thread.");
        }
    }

    private void ThrowIfUnavailable()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        RequireUiThread();
    }

    private IDispatcher? FindDispatcher() =>
        (Handler is null) && (Application.Current is null) && (Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread() is null) ? null : Dispatcher;

    //--------------------------------------------------------------------------------
    // Validation
    //--------------------------------------------------------------------------------

    internal static bool ValidatePadding(object? value, string name) =>
        ((value is not Thickness padding) || (IsNonNegative(padding.Left) && IsNonNegative(padding.Top) && IsNonNegative(padding.Right) && IsNonNegative(padding.Bottom))) ? true : throw new ArgumentOutOfRangeException(name, value, "Each side must be a finite value of zero or more.");

    private static bool IsNonNegative(double value) => Double.IsFinite(value) && (value >= 0);
}

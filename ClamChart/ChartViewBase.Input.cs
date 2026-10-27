namespace ClamChart;

using System.Diagnostics;
using System.Windows.Input;

using ClamChart.Input;
using ClamChart.Platform;

using Microsoft.Maui.Dispatching;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public abstract partial class ChartViewBase
{
    public static readonly BindableProperty TouchModeProperty = BindableProperty.Create(nameof(TouchMode), typeof(ChartTouchMode), typeof(ChartViewBase), ChartTouchMode.Auto, propertyChanged: OnInputModeChanged);
    public static readonly BindableProperty ItemTappedCommandProperty = BindableProperty.Create(nameof(ItemTappedCommand), typeof(ICommand), typeof(ChartViewBase));
    public static readonly BindableProperty ChartTappedCommandProperty = BindableProperty.Create(nameof(ChartTappedCommand), typeof(ICommand), typeof(ChartViewBase));
    public static readonly BindableProperty HapticFeedbackEnabledProperty = BindableProperty.Create(nameof(HapticFeedbackEnabled), typeof(bool), typeof(ChartViewBase), false);

    private const int InputTickMilliseconds = 50;

    private readonly ChartGestureController gesture = new();
    private IDispatcherTimer? inputTimer;
    private IChartPlatformBridge? platform;
    private SKPaint? pressedPaint;
    private Point pressPoint;
    private Point inputPoint;

    public event EventHandler<ChartItemEventArgs>? ItemTapped;

    public event EventHandler<ChartTappedEventArgs>? ChartTapped;

    public ChartTouchMode TouchMode
    {
        get => (ChartTouchMode)GetValue(TouchModeProperty);
        set => SetValue(TouchModeProperty, value);
    }

    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }

    public ICommand? ChartTappedCommand
    {
        get => (ICommand?)GetValue(ChartTappedCommandProperty);
        set => SetValue(ChartTappedCommandProperty, value);
    }

    public bool HapticFeedbackEnabled
    {
        get => (bool)GetValue(HapticFeedbackEnabledProperty);
        set => SetValue(HapticFeedbackEnabledProperty, value);
    }

    internal ChartTouchMode EffectiveTouchMode => IsBusy ? ChartTouchMode.PassThrough : TouchMode == ChartTouchMode.Auto ? ResolveTouchMode() : TouchMode;

    internal bool HasChartTapHandlers => (ChartTapped is not null) || (ChartTappedCommand is not null);

    internal bool HasItemTapHandlers => (ItemTapped is not null) || (ItemTappedCommand is not null);

    internal bool IsPressed { get; private set; }

    internal bool IsTracking { get; private set; }

    internal long InputGeneration { get; private set; }

    internal virtual ChartTrackballMode InputTrackballMode => ChartTrackballMode.None;

    private static double InputTime => Stopwatch.GetTimestamp() * 1000d / Stopwatch.Frequency;

    public void CancelInteraction()
    {
        ThrowIfUnavailable();
        CancelInput();
    }

    internal static void OnInputModeChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartViewBase)bindable).CancelInput();

    internal static void ExecuteCommand(ICommand? command, object parameter)
    {
        if ((command is not null) && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    internal virtual ChartTouchMode ResolveTouchMode() => HasChartTapHandlers ? ChartTouchMode.Interactive : ChartTouchMode.PassThrough;

    internal virtual bool IsInputTarget(Point point) => false;

    internal virtual void OnInputTap(Point start, Point end) => RaiseChartTapped(start);

    internal virtual void OnTrackStarted(Point point)
    {
    }

    internal virtual void OnTrackMoved(Point point)
    {
    }

    internal virtual void OnTrackEnded()
    {
    }

    internal virtual void OnInputCanceled()
    {
    }

    internal bool InvokeItemTapped(ChartItemEventArgs args)
    {
        ItemTapped?.Invoke(this, args);
        return args.Handled;
    }

    internal bool InvokeChartTapped(ChartTappedEventArgs args)
    {
        ChartTapped?.Invoke(this, args);
        return args.Handled;
    }

    internal bool RaiseChartTapped(Point position)
    {
        var args = new ChartTappedEventArgs(position);
        var generation = InputGeneration;
        if (!InvokeChartTapped(args) && (generation == InputGeneration))
        {
            ExecuteCommand(ChartTappedCommand, args);
        }

        return args.Handled;
    }

    internal bool HandleTouch(long id, SKTouchAction action, Point point, double milliseconds)
    {
        if (disposed || !IsEnabled || (Width <= 0) || (Height <= 0))
        {
            return false;
        }

        EnsureLayout();
        switch (action)
        {
            case SKTouchAction.Pressed:
                return Press(id, point, milliseconds);
            case SKTouchAction.Moved:
                return Move(id, point);
            case SKTouchAction.Released:
                return Release(id, point);
            case SKTouchAction.Cancelled:
                var active = gesture.State != ChartGestureState.Idle;
                CancelInput(true);
                return active;
            default:
                return false;
        }
    }

    internal void HandleInputTick(double milliseconds)
    {
        EnsureLayout();
        if (gesture.Tick(milliseconds) == ChartGestureAction.TrackStart)
        {
            StopInputTimer();
            SetParentIntercept(false);
            StartTracking(inputPoint);
        }
        else if (gesture.State != ChartGestureState.Pressed)
        {
            StopInputTimer();
        }
    }

    internal void CancelInput(bool releasePointers = false)
    {
        InputGeneration++;
        if (releasePointers)
        {
            gesture.Cancel();
        }
        else
        {
            gesture.Block();
        }

        StopInputTimer();
        SetParentIntercept(true);
        SetPressed(false);
        EndTracking();
        OnInputCanceled();
    }

    partial void AttachPlatform();

    private void DetachPlatform()
    {
        platform?.Dispose();
        platform = null;
    }

    private void SetParentIntercept(bool allow) => platform?.SetParentIntercept(allow);

    internal void PerformHaptic(ChartHaptic haptic)
    {
        if (HapticFeedbackEnabled)
        {
            platform?.PerformHaptic(haptic);
        }
    }

    private void OnChartTouch(object? sender, SKTouchEventArgs e)
    {
        if ((CanvasSize.Width <= 0) || (CanvasSize.Height <= 0))
        {
            return;
        }

        var point = new Point(e.Location.X * Width / CanvasSize.Width, e.Location.Y * Height / CanvasSize.Height);
        e.Handled = HandleTouch(e.Id, e.ActionType, point, InputTime);
    }

    private bool Press(long id, Point point, double milliseconds)
    {
        if (gesture.State != ChartGestureState.Idle)
        {
            if (gesture.Press(id, point, milliseconds, ChartTrackballMode.None) == ChartGestureAction.Canceled)
            {
                CancelInput();
            }

            return true;
        }

        var mode = EffectiveTouchMode;
        if ((mode == ChartTouchMode.PassThrough) || ((mode == ChartTouchMode.HitOnly) && !IsInputTarget(point)))
        {
            return false;
        }

        var trackball = InputTrackballMode;
        pressPoint = point;
        inputPoint = point;
        if (gesture.Press(id, point, milliseconds, trackball) == ChartGestureAction.TrackStart)
        {
            SetParentIntercept(false);
            StartTracking(point);
        }
        else if (trackball == ChartTrackballMode.LongPress)
        {
            StartInputTimer();
        }

        SetPressed((EffectiveDensity == ChartDensity.Minimal) && HasChartTapHandlers);
        return true;
    }

    private bool Move(long id, Point point)
    {
        if (gesture.State == ChartGestureState.Idle)
        {
            return false;
        }

        if (gesture.IsPrimary(id))
        {
            inputPoint = point;
        }

        var action = gesture.Move(id, point);
        if ((action == ChartGestureAction.Track) && IsTracking)
        {
            OnTrackMoved(point);
        }
        else if (action == ChartGestureAction.Yield)
        {
            SetParentIntercept(true);
            EndTracking();
        }

        if (gesture.State != ChartGestureState.Pressed)
        {
            StopInputTimer();
        }

        if (gesture.HasMoved || (gesture.State != ChartGestureState.Pressed))
        {
            SetPressed(false);
        }

        return true;
    }

    private bool Release(long id, Point point)
    {
        if (gesture.State == ChartGestureState.Idle)
        {
            return false;
        }

        var primary = gesture.IsPrimary(id);
        var inside = (point.X >= 0) && (point.Y >= 0) && (point.X <= Width) && (point.Y <= Height);
        var action = gesture.Release(id, point, inside);
        if (primary)
        {
            StopInputTimer();
            SetParentIntercept(true);
            SetPressed(false);
            EndTracking();
        }

        if (action == ChartGestureAction.Tap)
        {
            OnInputTap(pressPoint, point);
        }

        return true;
    }

    private void StartTracking(Point point)
    {
        IsTracking = true;
        OnTrackStarted(point);
    }

    private void EndTracking()
    {
        if (IsTracking)
        {
            IsTracking = false;
            OnTrackEnded();
        }
    }

    private void SetPressed(bool value)
    {
        if (IsPressed == value)
        {
            return;
        }

        IsPressed = value;
        if (!disposed)
        {
            InvalidateCanvas();
        }
    }

    private void DrawPressedOverlay(SKCanvas canvas, float width, float height)
    {
        if (!IsPressed)
        {
            return;
        }

        pressedPaint ??= new SKPaint { IsAntialias = false, Style = SKPaintStyle.Fill };
        pressedPaint.Color = EffectiveStyle.PressedOverlayColor.ToSKColor();
        canvas.DrawRect(0, 0, width, height, pressedPaint);
    }

    private void StartInputTimer()
    {
        if (inputTimer is null)
        {
            inputTimer = Dispatcher.CreateTimer();
            inputTimer.Interval = TimeSpan.FromMilliseconds(InputTickMilliseconds);
            inputTimer.Tick += OnInputTick;
        }

        inputTimer.Start();
    }

    private void OnInputTick(object? sender, EventArgs e)
    {
        if (disposed || (Handler is null) || !IsVisible || !IsEnabled)
        {
            CancelInput(true);
            return;
        }

        HandleInputTick(InputTime);
    }

    private void StopInputTimer() => inputTimer?.Stop();

    private void ReleaseInput()
    {
        if (inputTimer is not null)
        {
            inputTimer.Stop();
            inputTimer.Tick -= OnInputTick;
            inputTimer = null;
        }

        DetachPlatform();
        pressedPaint?.Dispose();
        pressedPaint = null;
    }
}

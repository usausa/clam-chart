namespace ClamChart.Input;

internal sealed class ChartGestureController
{
    public const double TapThreshold = 6;
    public const double MovementThreshold = 8;
    public const double VerticalBias = 1.25;
    public const double LongPressMilliseconds = 500;

    private readonly HashSet<long> pointers = [];
    private long primary;
    private Point origin;
    private double pressedAt;
    private ChartTrackballMode trackball;
    private bool decided;

    public ChartGestureState State { get; private set; }

    public bool HasMoved { get; private set; }

    public bool IsPrimary(long id) => (id == primary) && pointers.Contains(id);

    public ChartGestureAction Press(long id, Point point, double milliseconds, ChartTrackballMode trackballMode)
    {
        if (!pointers.Add(id) || (pointers.Count > 1))
        {
            Block();
            return ChartGestureAction.Canceled;
        }

        primary = id;
        origin = point;
        pressedAt = milliseconds;
        trackball = trackballMode;
        HasMoved = false;
        decided = trackballMode != ChartTrackballMode.Touch;
        if (trackballMode == ChartTrackballMode.Touch)
        {
            State = ChartGestureState.Tracking;
            return ChartGestureAction.TrackStart;
        }

        State = ChartGestureState.Pressed;
        return ChartGestureAction.None;
    }

    public ChartGestureAction Move(long id, Point point)
    {
        if (!IsPrimary(id) || (State is not (ChartGestureState.Pressed or ChartGestureState.Tracking)))
        {
            return ChartGestureAction.None;
        }

        var dx = Math.Abs(point.X - origin.X);
        var dy = Math.Abs(point.Y - origin.Y);
        HasMoved |= (dx >= TapThreshold) || (dy >= TapThreshold);
        var beyond = (dx >= MovementThreshold) || (dy >= MovementThreshold);
        if (State == ChartGestureState.Pressed)
        {
            if (beyond)
            {
                State = ChartGestureState.Moved;
            }

            return ChartGestureAction.None;
        }

        if (!decided && beyond)
        {
            decided = true;
            if (dy >= dx * VerticalBias)
            {
                State = ChartGestureState.Yielded;
                return ChartGestureAction.Yield;
            }
        }

        return ChartGestureAction.Track;
    }

    public ChartGestureAction Tick(double milliseconds)
    {
        if ((State != ChartGestureState.Pressed) || (trackball != ChartTrackballMode.LongPress) || ((milliseconds - pressedAt) < LongPressMilliseconds))
        {
            return ChartGestureAction.None;
        }

        State = ChartGestureState.Tracking;
        return ChartGestureAction.TrackStart;
    }

    public ChartGestureAction Release(long id, Point point, bool inside)
    {
        if (!pointers.Contains(id))
        {
            return ChartGestureAction.None;
        }

        var result = ChartGestureAction.None;
        if ((id == primary) && (State != ChartGestureState.Blocked))
        {
            if (Move(id, point) == ChartGestureAction.Yield)
            {
                result = ChartGestureAction.Yield;
            }
            else if (!HasMoved && inside && ((State == ChartGestureState.Pressed) || ((State == ChartGestureState.Tracking) && (trackball == ChartTrackballMode.Touch))))
            {
                result = ChartGestureAction.Tap;
            }
            else if (State == ChartGestureState.Tracking)
            {
                result = ChartGestureAction.TrackEnd;
            }
        }

        pointers.Remove(id);
        if (pointers.Count == 0)
        {
            Cancel();
        }

        return result;
    }

    public void Block()
    {
        State = pointers.Count > 0 ? ChartGestureState.Blocked : ChartGestureState.Idle;
    }

    public void Cancel()
    {
        pointers.Clear();
        State = ChartGestureState.Idle;
    }
}

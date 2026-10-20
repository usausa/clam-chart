namespace ClamChart.Input;

internal enum ChartGestureState
{
    Idle,
    Pressed,
    Moved,
    Tracking,
    Yielded,
    Blocked
}

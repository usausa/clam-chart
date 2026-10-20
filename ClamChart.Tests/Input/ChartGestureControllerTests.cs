namespace ClamChart.Tests.Input;

using ClamChart.Input;

public sealed class ChartGestureControllerTests
{
    [Fact]
    public void SmallMovementStaysATap()
    {
        // Arrange
        var input = new ChartGestureController();

        // Act & Assert
        Assert.Equal(ChartGestureAction.None, input.Press(1, new Point(50, 50), 0, ChartTrackballMode.None));
        Assert.Equal(ChartGestureState.Pressed, input.State);
        Assert.Equal(ChartGestureAction.None, input.Move(1, new Point(55, 45)));
        Assert.False(input.HasMoved);
        Assert.Equal(ChartGestureAction.Tap, input.Release(1, new Point(55, 45), true));
        Assert.Equal(ChartGestureState.Idle, input.State);
    }

    [Fact]
    public void MovingSixIsNoLongerATap()
    {
        // Arrange
        var input = new ChartGestureController();
        input.Press(1, new Point(50, 50), 0, ChartTrackballMode.None);

        // Act & Assert
        Assert.Equal(ChartGestureAction.None, input.Move(1, new Point(50, 56)));
        Assert.True(input.HasMoved);
        Assert.Equal(ChartGestureState.Pressed, input.State);
        Assert.Equal(ChartGestureAction.None, input.Release(1, new Point(50, 50), true));
    }

    [Fact]
    public void ReleasingOutsideIsNotATap()
    {
        // Arrange
        var input = new ChartGestureController();
        input.Press(1, new Point(50, 50), 0, ChartTrackballMode.None);

        // Act & Assert
        Assert.Equal(ChartGestureAction.None, input.Release(1, new Point(51, 50), false));
        Assert.Equal(ChartGestureState.Idle, input.State);
    }

    [Fact]
    public void LongPressStartsTrackingAtFiveHundredMilliseconds()
    {
        // Arrange
        var input = new ChartGestureController();
        input.Press(1, new Point(50, 50), 1000, ChartTrackballMode.LongPress);

        // Act & Assert
        Assert.Equal(ChartGestureAction.None, input.Tick(1499));
        Assert.Equal(ChartGestureAction.None, input.Move(1, new Point(57, 43)));
        Assert.Equal(ChartGestureAction.TrackStart, input.Tick(1500));
        Assert.Equal(ChartGestureState.Tracking, input.State);
        Assert.Equal(ChartGestureAction.None, input.Tick(1600));
        Assert.Equal(ChartGestureAction.Track, input.Move(1, new Point(57, 120)));
        Assert.Equal(ChartGestureAction.TrackEnd, input.Release(1, new Point(57, 120), true));
        Assert.Equal(ChartGestureState.Idle, input.State);
    }

    [Fact]
    public void MovingEightBeforeTheLongPressCancelsIt()
    {
        // Arrange
        var input = new ChartGestureController();
        input.Press(1, new Point(50, 50), 0, ChartTrackballMode.LongPress);

        // Act & Assert
        Assert.Equal(ChartGestureAction.None, input.Move(1, new Point(58, 50)));
        Assert.Equal(ChartGestureState.Moved, input.State);
        Assert.Equal(ChartGestureAction.None, input.Tick(600));
        Assert.Equal(ChartGestureAction.None, input.Release(1, new Point(58, 50), true));
    }

    [Fact]
    public void LongPressIsNotRecognizedWithoutTheTrackball()
    {
        // Arrange
        var input = new ChartGestureController();
        input.Press(1, new Point(50, 50), 0, ChartTrackballMode.None);

        // Act & Assert
        Assert.Equal(ChartGestureAction.None, input.Tick(800));
        Assert.Equal(ChartGestureAction.Tap, input.Release(1, new Point(50, 50), true));
    }

    [Fact]
    public void TouchModeTracksAtOnceAndYieldsToAVerticalStart()
    {
        // Arrange
        var input = new ChartGestureController();

        // Act & Assert
        Assert.Equal(ChartGestureAction.TrackStart, input.Press(1, new Point(50, 50), 0, ChartTrackballMode.Touch));
        Assert.Equal(ChartGestureState.Tracking, input.State);
        Assert.Equal(ChartGestureAction.Track, input.Move(1, new Point(52, 55)));
        Assert.Equal(ChartGestureAction.Yield, input.Move(1, new Point(53, 60)));
        Assert.Equal(ChartGestureState.Yielded, input.State);
        Assert.Equal(ChartGestureAction.None, input.Move(1, new Point(90, 60)));
        Assert.Equal(ChartGestureAction.None, input.Release(1, new Point(90, 60), true));
        Assert.Equal(ChartGestureState.Idle, input.State);
    }

    [Fact]
    public void TouchModeKeepsTrackingAfterAHorizontalStart()
    {
        // Arrange
        var input = new ChartGestureController();
        input.Press(1, new Point(50, 50), 0, ChartTrackballMode.Touch);

        // Act & Assert
        Assert.Equal(ChartGestureAction.Track, input.Move(1, new Point(60, 56)));
        Assert.Equal(ChartGestureAction.Track, input.Move(1, new Point(60, 120)));
        Assert.Equal(ChartGestureState.Tracking, input.State);
        Assert.Equal(ChartGestureAction.TrackEnd, input.Release(1, new Point(60, 120), true));
    }

    [Fact]
    public void QuickTouchInTouchModeIsATap()
    {
        // Arrange
        var input = new ChartGestureController();
        input.Press(1, new Point(50, 50), 0, ChartTrackballMode.Touch);

        // Act & Assert
        Assert.Equal(ChartGestureAction.Tap, input.Release(1, new Point(52, 51), true));
    }

    [Fact]
    public void SecondPointerBlocksUntilAllPointersAreReleased()
    {
        // Arrange
        var input = new ChartGestureController();
        input.Press(1, new Point(50, 50), 0, ChartTrackballMode.Touch);

        // Act & Assert
        Assert.Equal(ChartGestureAction.Canceled, input.Press(2, new Point(80, 50), 10, ChartTrackballMode.None));
        Assert.Equal(ChartGestureState.Blocked, input.State);
        Assert.Equal(ChartGestureAction.None, input.Move(1, new Point(70, 50)));
        Assert.Equal(ChartGestureAction.None, input.Release(1, new Point(70, 50), true));
        Assert.Equal(ChartGestureState.Blocked, input.State);
        Assert.Equal(ChartGestureAction.None, input.Release(2, new Point(80, 50), true));
        Assert.Equal(ChartGestureState.Idle, input.State);
    }

    [Fact]
    public void BlockWithoutPointersReturnsToIdleAndCancelForgetsThem()
    {
        // Arrange
        var input = new ChartGestureController();

        // Act & Assert
        input.Block();
        Assert.Equal(ChartGestureState.Idle, input.State);
        input.Press(1, new Point(50, 50), 0, ChartTrackballMode.None);
        input.Cancel();
        Assert.Equal(ChartGestureState.Idle, input.State);
        Assert.False(input.IsPrimary(1));
        Assert.Equal(ChartGestureAction.None, input.Release(1, new Point(50, 50), true));
    }
}

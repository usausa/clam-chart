namespace ClamChart.Tests;

using Microsoft.Maui;

using SkiaSharp.Views.Maui;

public sealed class ChartViewBaseTests
{
    [Fact]
    public void CommonPropertiesHaveNoValueByDefault()
    {
        // Arrange
        using var view = new TestChartView();

        // Act & Assert
        Assert.Null(view.Palette);
        Assert.Null(view.ContentPadding);
        Assert.Null(view.Culture);
        Assert.Null(view.EmptyText);
        Assert.Null(view.ChartTappedCommand);
    }

    [Fact]
    public void ContentPaddingRejectsNegativeAndNonFiniteSides()
    {
        // Arrange
        using var view = new TestChartView();
        view.ContentPadding = new Thickness(1, 2, 3, 4);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.ContentPadding = new Thickness(-1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => view.ContentPadding = new Thickness(0, Double.NaN, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => view.ContentPadding = new Thickness(0, 0, Double.PositiveInfinity, 0));
        Assert.Equal(new Thickness(1, 2, 3, 4), view.ContentPadding);

        // Act & Assert
        view.ContentPadding = null;
        Assert.Null(view.ContentPadding);
    }

    [Fact]
    public void DataChangesInTheSameLoopAreUpdatedOnce()
    {
        // Arrange
        var dispatcher = TestDispatcher.Current;
        dispatcher.RunPending();
        using var view = new TestChartView();

        // Act
        view.DataInput = 1;
        view.DataInput = 2;
        view.DataInput = 3;

        // Assert
        Assert.Equal(1, dispatcher.PendingCount);
        Assert.Equal(0, view.DataUpdates);

        // Act
        dispatcher.RunPending();

        // Assert
        Assert.Equal(1, view.DataUpdates);
        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public void PaintBeforeTheDispatchedUpdateKeepsThePreviousData()
    {
        // Arrange
        var dispatcher = TestDispatcher.Current;
        dispatcher.RunPending();
        using var view = new TestChartView();
        view.Paint(200, 100);

        // Act
        view.DataInput = 1;
        view.Paint(200, 100);

        // Assert
        Assert.Equal(1, view.DataUpdates);
        Assert.Equal(2, view.Renders);

        // Act
        dispatcher.RunPending();
        view.Paint(200, 100);

        // Assert
        Assert.Equal(2, view.DataUpdates);
        Assert.Equal(3, view.LayoutUpdates);
        Assert.Equal(3, view.Renders);
    }

    [Fact]
    public void LayoutIsUpdatedOnlyWhenALayoutInputOrTheSizeChanges()
    {
        // Arrange
        using var view = new TestChartView();
        view.Paint(200, 100);
        view.Paint(200, 100);

        // Assert
        Assert.Equal(1, view.LayoutUpdates);
        Assert.Equal(200f, view.LastLayoutWidth);
        Assert.Equal(100f, view.LastLayoutHeight);

        // Act
        view.LayoutInput = 1;
        view.Paint(200, 100);

        // Assert
        Assert.Equal(2, view.LayoutUpdates);

        // Act
        view.Paint(300, 100);

        // Assert
        Assert.Equal(3, view.LayoutUpdates);
        Assert.Equal(300f, view.LastLayoutWidth);

        // Act
        view.Culture = CultureInfo.InvariantCulture;
        view.ContentPadding = new Thickness(4);
        view.Paint(300, 100);

        // Assert
        Assert.Equal(4, view.LayoutUpdates);

        // Act
        view.EmptyText = "No data";
        view.Palette = [Colors.Red];
        view.Paint(300, 100);

        // Assert
        Assert.Equal(4, view.LayoutUpdates);
        Assert.Equal(6, view.Renders);
    }

    [Fact]
    public void StyleChangesResetTheRendererAndTheLayout()
    {
        // Arrange
        using var view = new TestChartView();
        view.Paint(200, 100);

        // Act
        view.ChartStyle = ChartStyles.Dark;
        view.Paint(200, 100);

        // Assert
        Assert.Equal(1, view.RendererResets);
        Assert.Equal(2, view.LayoutUpdates);
    }

    [Fact]
    public void ChartStyleIsLightByDefaultAndRejectsNullAndInvalidStyles()
    {
        // Arrange
        using var view = new TestChartView();

        // Act & Assert
        Assert.Same(ChartStyles.Light, view.ChartStyle);
        Assert.Throws<ArgumentNullException>(() => view.ChartStyle = null!);
        Assert.Throws<ArgumentException>(() => view.ChartStyle = ChartStyles.Light with { LineThickness = 0 });
        Assert.Same(ChartStyles.Light, view.ChartStyle);
    }

    [Fact]
    public void InvalidateRevalidatesAStyleChangedInPlace()
    {
        // Arrange
        using var view = new TestChartView();
        var style = ChartStyles.Light with { };
        view.ChartStyle = style;

        // Act
        style.MarkerSize = -1;

        // Assert
        Assert.Throws<ArgumentException>(view.Invalidate);
    }

    [Fact]
    public void EffectiveDensityFollowsTheSizeUnlessTheDensityIsFixed()
    {
        // Arrange
        using var view = new TestChartView();

        // Act & Assert
        view.Frame = new Rect(0, 0, 400, 300);
        Assert.Equal(ChartDensity.Full, view.EffectiveDensity);
        view.Frame = new Rect(0, 0, 320, 200);
        Assert.Equal(ChartDensity.Compact, view.EffectiveDensity);
        view.Frame = new Rect(0, 0, 160, 96);
        Assert.Equal(ChartDensity.Minimal, view.EffectiveDensity);

        // Act & Assert
        view.Density = ChartDensity.Full;
        Assert.Equal(ChartDensity.Full, view.EffectiveDensity);

        // Act & Assert
        view.Density = ChartDensity.Auto;
        view.Frame = new Rect(0, 0, 400, 300);
        view.ChartStyle = ChartStyles.Light with { CompactSize = new Size(500, 400) };
        Assert.Equal(ChartDensity.Compact, view.EffectiveDensity);
    }

    [Fact]
    public void ContentPaddingDefaultsToTheDensity()
    {
        // Arrange
        using var view = new TestChartView();

        // Act & Assert
        view.Frame = new Rect(0, 0, 400, 300);
        Assert.Equal(new Thickness(8), view.EffectiveContentPadding);
        view.Frame = new Rect(0, 0, 200, 150);
        Assert.Equal(new Thickness(4), view.EffectiveContentPadding);
        view.Frame = new Rect(0, 0, 100, 50);
        Assert.Equal(new Thickness(0), view.EffectiveContentPadding);

        // Act & Assert
        view.ContentPadding = new Thickness(1, 2, 3, 4);
        Assert.Equal(new Thickness(1, 2, 3, 4), view.EffectiveContentPadding);
    }

    [Fact]
    public void DensityChangesInvalidateTheLayout()
    {
        // Arrange
        using var view = new TestChartView();
        view.Paint(200, 100);

        // Act
        view.Density = ChartDensity.Compact;
        view.Paint(200, 100);

        // Assert
        Assert.Equal(2, view.LayoutUpdates);
    }

    [Fact]
    public void InvalidateResetsTheRendererAndTheLayout()
    {
        // Arrange
        using var view = new TestChartView();
        view.Paint(200, 100);

        // Act
        view.Invalidate();
        view.Paint(200, 100);

        // Assert
        Assert.Equal(1, view.RendererResets);
        Assert.Equal(2, view.LayoutUpdates);
        Assert.Equal(1, view.DataUpdates);
    }

    [Fact]
    public void RenderReceivesTheSizeInDipOnAScaledCanvas()
    {
        // Arrange
        using var view = new TestChartView();

        // Act
        view.Paint(200, 100);

        // Assert
        Assert.Equal(200f, view.LastRenderWidth);
        Assert.Equal(2f, view.LastRenderScale);
    }

    [Fact]
    public void NothingIsRenderedWithoutASize()
    {
        // Arrange
        using var view = new TestChartView();

        // Act
        view.Paint(0, 0);

        // Assert
        Assert.Equal(0, view.Renders);
        Assert.Equal(0, view.DataUpdates);
    }

    [Fact]
    public void FrameRenderedReportsEachFrame()
    {
        // Arrange
        using var view = new TestChartView();
        var frames = new List<ChartFrameEventArgs>();
        view.FrameRendered += (_, e) => frames.Add(e);

        // Act
        view.Paint(200, 100);
        view.Paint(200, 100);

        // Assert
        Assert.Equal(2, frames.Count);
        Assert.Equal(42, frames[0].PointCount);
        Assert.False(frames[0].Animating);
        Assert.True(frames[0].ElapsedMilliseconds >= 0);
    }

    [Fact]
    public void ChartTappedRaisesTheEventAndThenTheCommand()
    {
        // Arrange
        using var view = new TestChartView();
        var calls = new List<string>();
        view.ChartTapped += (_, e) => calls.Add($"event {e.Position.X},{e.Position.Y}");
        view.ChartTappedCommand = new Command(parameter => calls.Add($"command {((ChartTappedEventArgs)parameter).Position.X}"));

        // Act
        var handled = view.RaiseChartTapped(new Point(3, 4));

        // Assert
        Assert.False(handled);
        Assert.Equal(["event 3,4", "command 3"], calls);
    }

    [Fact]
    public void HandledChartTapSkipsTheCommand()
    {
        // Arrange
        using var view = new TestChartView();
        var executed = false;
        view.ChartTapped += (_, e) => e.Handled = true;
        view.ChartTappedCommand = new Command(() => executed = true);

        // Act
        var handled = view.RaiseChartTapped(new Point(1, 1));

        // Assert
        Assert.True(handled);
        Assert.False(executed);
    }

    [Fact]
    public void ChartTappedCommandThatCannotExecuteIsSkipped()
    {
        // Arrange
        using var view = new TestChartView();
        var executed = false;
        view.ChartTappedCommand = new Command(() => executed = true, static () => false);

        // Act
        view.RaiseChartTapped(new Point(1, 1));

        // Assert
        Assert.False(executed);
    }

    [Fact]
    public void PublicMethodsReachTheChart()
    {
        // Arrange
        using var view = new TestChartView();

        // Act
        view.CancelInteraction();
        view.PlayEntranceAnimation();
        view.CompleteAnimations();

        // Assert
        Assert.Equal(1, view.InputCancels);
        Assert.Equal(1, view.EntranceStarts);
        Assert.Equal(1, view.AnimationFinishes);
    }

    [Fact]
    public void HidingOrDisablingCancelsInputAndHidingCompletesAnimations()
    {
        // Arrange
        using var view = new TestChartView();

        // Act
        view.IsVisible = false;

        // Assert
        Assert.Equal(1, view.InputCancels);
        Assert.Equal(1, view.AnimationFinishes);

        // Act
        view.IsVisible = true;
        view.IsEnabled = false;

        // Assert
        Assert.Equal(2, view.InputCancels);
        Assert.Equal(1, view.AnimationFinishes);
    }

    [Fact]
    public void DisposeReleasesTheChartOnceAndBlocksTheMethods()
    {
        // Arrange
        var view = new TestChartView();

        // Act
        view.Dispose();
        view.Dispose();

        // Assert
        Assert.Equal(1, view.InputCancels);
        Assert.Equal(1, view.AnimationFinishes);
        Assert.Equal(1, view.Detaches);
        Assert.Equal(1, view.RendererResets);
        Assert.Throws<ObjectDisposedException>(view.Invalidate);
        Assert.Throws<ObjectDisposedException>(view.CancelInteraction);
        Assert.Throws<ObjectDisposedException>(view.PlayEntranceAnimation);
        Assert.Throws<ObjectDisposedException>(view.CompleteAnimations);
    }

    [Fact]
    public void ChangesAfterDisposeAreIgnored()
    {
        // Arrange
        var dispatcher = TestDispatcher.Current;
        dispatcher.RunPending();
        var view = new TestChartView();
        view.Dispose();

        // Act
        view.DataInput = 1;
        view.EmptyText = "No data";
        view.Paint(200, 100);

        // Assert
        Assert.Equal(0, dispatcher.PendingCount);
        Assert.Equal(0, view.DataUpdates);
        Assert.Equal(0, view.Renders);
    }

    [Fact]
    public async Task ChangesFromAnotherThreadAreRejected()
    {
        // Arrange
        using var view = new TestChartView();
        view.ChangeDataInput();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(view.ChangeDataInput, TestContext.Current.CancellationToken)).ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(view.Invalidate, TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    [Fact]
    public void TapReachesChartTappedOnlyWhenSomethingListens()
    {
        // Arrange
        using var view = new TestChartView();
        view.Paint(200, 120);
        var taps = new List<Point>();

        // Act & Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        Assert.False(view.HandleTouch(1, SKTouchAction.Pressed, new Point(10, 20), 0));
        view.ChartTapped += (_, e) => taps.Add(e.Position);
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
        Assert.True(view.HandleTouch(1, SKTouchAction.Pressed, new Point(10, 20), 0));
        Assert.True(view.HandleTouch(1, SKTouchAction.Moved, new Point(12, 21), 5));
        Assert.True(view.HandleTouch(1, SKTouchAction.Released, new Point(12, 21), 10));
        Assert.Equal([new Point(10, 20)], taps);
        Assert.False(view.HandleTouch(1, SKTouchAction.Released, new Point(12, 21), 20));
    }

    [Fact]
    public void ResizingCancelsTheTap()
    {
        // Arrange
        using var view = new TestChartView();
        view.Paint(200, 120);
        var taps = 0;
        view.ChartTapped += (_, _) => taps++;
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(10, 20), 0);
        var cancels = view.InputCancels;

        // Act
        view.Frame = new Rect(0, 0, 300, 200);
        var handled = view.HandleTouch(1, SKTouchAction.Released, new Point(10, 20), 10);

        // Assert
        Assert.True(handled);
        Assert.Equal(0, taps);
        Assert.Equal(cancels + 1, view.InputCancels);
    }

    [Fact]
    public void DisabledChartIgnoresTouches()
    {
        // Arrange
        using var view = new TestChartView();
        view.Paint(200, 120);
        view.TouchMode = ChartTouchMode.Interactive;
        view.IsEnabled = false;

        // Act & Assert
        Assert.False(view.HandleTouch(1, SKTouchAction.Pressed, new Point(10, 20), 0));
    }

    [Fact]
    public async Task ChangesWithoutAnyDispatcherAreAppliedAtOnce()
    {
        // Act & Assert
        await TestDispatcherProvider.RunWithoutDispatcherAsync(static () =>
        {
            using var view = new TestChartView();
            view.DataInput = 1;
            view.Invalidate();
            Assert.Equal(1, view.DataUpdates);
            Assert.Equal(1, view.RendererResets);
        }).ConfigureAwait(true);
    }
}

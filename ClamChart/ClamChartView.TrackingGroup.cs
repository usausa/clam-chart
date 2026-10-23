namespace ClamChart;

using ClamChart.Layout;

public partial class ClamChartView
{
    public static readonly BindableProperty TrackingGroupProperty = BindableProperty.Create(nameof(TrackingGroup), typeof(string), typeof(ClamChartView), propertyChanged: OnTrackingGroupChanged);
    public static readonly BindableProperty TrackingGroupModeProperty = BindableProperty.Create(nameof(TrackingGroupMode), typeof(ChartTrackingGroupMode), typeof(ClamChartView), ChartTrackingGroupMode.Crosshair, propertyChanged: OnTrackingGroupModeChanged);

    private static readonly Dictionary<string, List<WeakReference<ClamChartView>>> TrackingGroups = [with(StringComparer.Ordinal)];

    private WeakReference<ClamChartView>? trackingReference;

    public string? TrackingGroup
    {
        get => (string?)GetValue(TrackingGroupProperty);
        set => SetValue(TrackingGroupProperty, value);
    }

    public ChartTrackingGroupMode TrackingGroupMode
    {
        get => (ChartTrackingGroupMode)GetValue(TrackingGroupModeProperty);
        set => SetValue(TrackingGroupModeProperty, value);
    }

    private static void OnTrackingGroupChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        view.LeaveTrackingGroup((string?)oldValue);
        view.JoinTrackingGroup((string?)newValue);
    }

    private static void OnTrackingGroupModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        if (view.interaction.IsFollowing)
        {
            view.UpdateTrackingTexts();
            view.InvalidateCanvas();
        }
    }

    private void JoinTrackingGroup(string? group)
    {
        if (String.IsNullOrEmpty(group))
        {
            return;
        }

        if (!TrackingGroups.TryGetValue(group, out var members))
        {
            members = [];
            TrackingGroups.Add(group, members);
        }

        trackingReference ??= new WeakReference<ClamChartView>(this);
        members.Add(trackingReference);
    }

    private void LeaveTrackingGroup(string? group)
    {
        if (interaction.IsFollowing)
        {
            interaction.ClearTracking();
            InvalidateCanvas();
        }

        if (String.IsNullOrEmpty(group) || (trackingReference is null) || !TrackingGroups.TryGetValue(group, out var members))
        {
            return;
        }

        members.Remove(trackingReference);
        if (members.Count == 0)
        {
            TrackingGroups.Remove(group);
        }
    }

    private void ShareTracking()
    {
        var group = TrackingGroup;
        if (String.IsNullOrEmpty(group) || !TrackingGroups.TryGetValue(group, out var members))
        {
            return;
        }

        var layout = ChartLayout;
        var kind = layout.XAxis.Options.Kind;
        var x = interaction.IsTracking && !interaction.IsFollowing && !interaction.IsCrosshair ? GetSharedX() : Double.NaN;
        var active = Double.IsFinite(x);
        var category = active && (kind == ChartAxisKind.Category) ? GetCategoryName(x) : null;
        var offset = category is null ? 0 : x - Math.Round(x);
        var across = active ? GetAcrossRatio() : Double.NaN;
        var window = Window;
        for (var i = members.Count - 1; i >= 0; i--)
        {
            if (!members[i].TryGetTarget(out var member))
            {
                members.RemoveAt(i);
            }
            else if ((member != this) && (member.Window == window))
            {
                member.FollowTracking(active, kind, x, category, offset, across);
            }
        }
    }

    private double GetSharedX()
    {
        var layout = ChartLayout;
        if (!interaction.Snaps)
        {
            var plot = layout.PlotBounds;
            var point = interaction.TrackPoint;
            return layout.XAxis.ToValue(layout.Orientation == ChartOrientation.Horizontal ? Math.Clamp(point.Y, plot.Y, plot.Bottom) : Math.Clamp(point.X, plot.X, plot.Right));
        }

        return interaction.LeadSeries >= 0 ? layout.Geometries[interaction.LeadSeries].X[interaction.LeadIndex] : Double.NaN;
    }

    private double GetAcrossRatio()
    {
        var layout = ChartLayout;
        var plot = layout.PlotBounds;
        var point = interaction.TrackPoint;
        var ratio = layout.Orientation == ChartOrientation.Horizontal
            ? (plot.Width > 0 ? (point.X - plot.X) / plot.Width : 0)
            : (plot.Height > 0 ? (point.Y - plot.Y) / plot.Height : 0);
        return Math.Clamp(ratio, 0, 1);
    }

    private string? GetCategoryName(double position)
    {
        var categories = ChartLayout.Categories;
        var index = (int)Math.Round(position);
        return (index >= 0) && (index < categories.Count) ? categories[index] : null;
    }

    private void FollowTracking(bool active, ChartAxisKind kind, double x, string? category, double offset, double across)
    {
        if (interaction.IsTracking && !interaction.IsFollowing)
        {
            return;
        }

        if (!active)
        {
            if (interaction.IsFollowing)
            {
                interaction.ClearTracking();
                InvalidateCanvas();
            }

            return;
        }

        EnsureLayout();
        var started = !interaction.IsFollowing;
        var line = interaction.FollowValue;
        var point = interaction.TrackPoint;
        interaction.IsTracking = true;
        interaction.IsFollowing = true;
        interaction.FollowKind = kind;
        interaction.FollowX = x;
        interaction.FollowCategory = category;
        interaction.FollowOffset = offset;
        interaction.FollowAcross = across;
        var changed = ResolveFollowing() || started;
        if (changed)
        {
            UpdateTrackingTexts();
        }

        var moved = (!interaction.Snaps && !line.Equals(interaction.FollowValue)) || ((interaction.TooltipHeader is not null) && (point != interaction.TrackPoint));
        if (changed || moved)
        {
            InvalidateCanvas();
        }
    }

    private bool ResolveFollowing()
    {
        interaction.IsCrosshair = false;
        var axis = ChartLayout.XAxis;
        var value = Double.NaN;
        if ((axis.Options.Kind == interaction.FollowKind) && !IsScatterOnly())
        {
            value = axis.Options.Kind == ChartAxisKind.Category ? FindCategory(interaction.FollowCategory) + interaction.FollowOffset : interaction.FollowX;
        }

        interaction.FollowValue = Double.IsFinite(value) && axis.IsDisplayed(value) ? value : Double.NaN;
        var changed = ResolveTrackingAt(interaction.FollowValue);
        PlaceFollowingPoint();
        return changed;
    }

    private void PlaceFollowingPoint()
    {
        var layout = ChartLayout;
        var lead = interaction.LeadSeries;
        var value = interaction.FollowValue;
        if (interaction.Snaps)
        {
            value = lead >= 0 ? layout.Geometries[lead].X[interaction.LeadIndex] : Double.NaN;
        }

        if (!Double.IsFinite(value))
        {
            return;
        }

        var plot = layout.PlotBounds;
        var along = layout.XAxis.ToPixel(value);
        var ratio = Double.IsFinite(interaction.FollowAcross) ? interaction.FollowAcross : 0;
        interaction.TrackPoint = layout.Orientation == ChartOrientation.Horizontal
            ? new Point(plot.X + (ratio * plot.Width), along)
            : new Point(along, plot.Y + (ratio * plot.Height));
    }

    private double FindCategory(string? name)
    {
        var categories = ChartLayout.Categories;
        for (var i = 0; i < categories.Count; i++)
        {
            if (String.Equals(categories[i], name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return Double.NaN;
    }
}

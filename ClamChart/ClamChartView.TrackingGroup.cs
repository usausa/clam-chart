namespace ClamChart;

using ClamChart.Layout;

public partial class ClamChartView
{
    public static readonly BindableProperty TrackingGroupProperty = BindableProperty.Create(nameof(TrackingGroup), typeof(string), typeof(ClamChartView), propertyChanged: OnTrackingGroupChanged);

    private static readonly Dictionary<string, List<WeakReference<ClamChartView>>> TrackingGroups = [with(StringComparer.Ordinal)];

    private WeakReference<ClamChartView>? trackingReference;

    public string? TrackingGroup
    {
        get => (string?)GetValue(TrackingGroupProperty);
        set => SetValue(TrackingGroupProperty, value);
    }

    private static void OnTrackingGroupChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        view.LeaveTrackingGroup((string?)oldValue);
        view.JoinTrackingGroup((string?)newValue);
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
        var active = interaction.IsTracking && !interaction.IsFollowing && !interaction.IsCrosshair && (interaction.LeadSeries >= 0);
        var x = active ? layout.Geometries[interaction.LeadSeries].X[interaction.LeadIndex] : Double.NaN;
        var category = active && (kind == ChartAxisKind.Category) ? GetCategoryName(x) : null;
        var window = Window;
        for (var i = members.Count - 1; i >= 0; i--)
        {
            if (!members[i].TryGetTarget(out var member))
            {
                members.RemoveAt(i);
            }
            else if ((member != this) && (member.Window == window))
            {
                member.FollowTracking(active, kind, x, category);
            }
        }
    }

    private string? GetCategoryName(double position)
    {
        var categories = ChartLayout.Categories;
        var index = (int)Math.Round(position);
        return (index >= 0) && (index < categories.Count) ? categories[index] : null;
    }

    private void FollowTracking(bool active, ChartAxisKind kind, double x, string? category)
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
        interaction.IsTracking = true;
        interaction.IsFollowing = true;
        interaction.FollowKind = kind;
        interaction.FollowX = x;
        interaction.FollowCategory = category;
        ResolveFollowing();
        InvalidateCanvas();
    }

    private bool ResolveFollowing()
    {
        interaction.IsCrosshair = false;
        var axis = ChartLayout.XAxis;
        var value = Double.NaN;
        if ((axis.Options.Kind == interaction.FollowKind) && !IsScatterOnly())
        {
            value = axis.Options.Kind == ChartAxisKind.Category ? FindCategory(interaction.FollowCategory) : interaction.FollowX;
        }

        return ResolveTrackingAt(Double.IsFinite(value) && axis.IsDisplayed(value) ? value : Double.NaN);
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

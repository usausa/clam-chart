namespace ClamChart;

using ClamChart.Data;

public partial class ClamChartView
{
    private readonly HashSet<string> categoryNames = [with(StringComparer.Ordinal)];
    private double categoryExtent = -1;
    private int measuredCategories = -1;

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var size = base.MeasureOverride(widthConstraint, heightConstraint);
        if (GetCategoryWidth() is not { } width)
        {
            return size;
        }

        measuredCategories = CountCategories();
        var length = (measuredCategories * width) + GetCategoryExtent();
        return Orientation == ChartOrientation.Horizontal ? new Size(size.Width, length) : new Size(length, size.Height);
    }

    private double? GetCategoryWidth()
    {
        if (XAxis is not CategoryAxis { CategoryWidth: { } width })
        {
            return null;
        }

        var request = Orientation == ChartOrientation.Horizontal ? HeightRequest : WidthRequest;
        return request < 0 ? width : null;
    }

    private double GetCategoryExtent()
    {
        if (categoryExtent >= 0)
        {
            return categoryExtent;
        }

        var padding = EffectiveContentPadding;
        return Orientation == ChartOrientation.Horizontal ? padding.VerticalThickness : padding.HorizontalThickness;
    }

    private void UpdateCategoryExtent(float width, float height)
    {
        if (GetCategoryWidth() is null)
        {
            if (categoryExtent >= 0)
            {
                categoryExtent = -1;
                InvalidateMeasure();
            }

            return;
        }

        var plot = ChartLayout.PlotBounds;
        var extent = Orientation == ChartOrientation.Horizontal ? height - plot.Height : width - plot.Width;
        if ((Math.Abs(extent - categoryExtent) > 0.5) || (CountCategories() != measuredCategories))
        {
            categoryExtent = extent;
            InvalidateMeasure();
        }
    }

    private int CountCategories()
    {
        categoryNames.Clear();
        var empty = false;
        var positions = 0;
        foreach (var series in attached)
        {
            var data = series.Snapshot;
            if (data.XKind != ChartXKind.Category)
            {
                positions = Math.Max(positions, data.Count);
                continue;
            }

            for (var i = 0; i < data.Count; i++)
            {
                if (data.GetCategory(i) is { } name)
                {
                    categoryNames.Add(name);
                }
                else
                {
                    empty = true;
                }
            }
        }

        return Math.Max(categoryNames.Count + (empty ? 1 : 0), positions);
    }
}

namespace ClamChart;

using System.Globalization;

using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Rendering;

public partial class ClamChartView
{
    public static readonly BindableProperty XAxisProperty = BindableProperty.Create(nameof(XAxis), typeof(ChartAxis), typeof(ClamChartView), propertyChanged: OnXAxisChanged);
    public static readonly BindableProperty YAxisProperty = BindableProperty.Create(nameof(YAxis), typeof(NumericAxis), typeof(ClamChartView), propertyChanged: OnValueAxisChanged);
    public static readonly BindableProperty SecondaryYAxisProperty = BindableProperty.Create(nameof(SecondaryYAxis), typeof(NumericAxis), typeof(ClamChartView), propertyChanged: OnValueAxisChanged);

    private const int CompactXLabelCount = 2;

    private const int CompactYLabelCount = 3;

    private readonly NumericAxis defaultNumericAxis = new() { NiceRange = false };
    private readonly DateTimeAxis defaultTimeAxis = new();
    private readonly CategoryAxis defaultCategoryAxis = new();
    private readonly NumericAxis defaultYAxis = new();
    private readonly NumericAxis defaultSecondaryYAxis = new();
    private ChartAxis? effectiveXAxis;
    private NumericAxis? effectiveSecondaryAxis;

    public ChartAxis? XAxis
    {
        get => (ChartAxis?)GetValue(XAxisProperty);
        set => SetValue(XAxisProperty, value);
    }

    public NumericAxis? YAxis
    {
        get => (NumericAxis?)GetValue(YAxisProperty);
        set => SetValue(YAxisProperty, value);
    }

    public NumericAxis? SecondaryYAxis
    {
        get => (NumericAxis?)GetValue(SecondaryYAxisProperty);
        set => SetValue(SecondaryYAxisProperty, value);
    }

    private static void OnXAxisChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        view.ReplaceAxis((ChartAxis?)oldValue, (ChartAxis?)newValue);
        view.InvalidateData();
        view.InvalidateMeasure();
    }

    private static void OnValueAxisChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        view.ReplaceAxis((ChartAxis?)oldValue, (ChartAxis?)newValue);
        view.InvalidateLayout();
    }

    private static void ReportInvertedBounds(ChartAxis? axis, AxisLayoutOptions? options)
    {
        if ((axis is null) || (options is null) || axis.InvertedBoundsReported || !AxisRangeResolver.HasInvertedBounds(options.Range))
        {
            return;
        }

        axis.InvertedBoundsReported = true;
        ChartDiagnostics.Warn("The axis Maximum is not greater than Minimum, so the automatic range is used.");
    }

    private void ReplaceAxis(ChartAxis? previous, ChartAxis? axis)
    {
        if (previous is not null)
        {
            previous.Owner = null;
            RemoveLogicalChild(previous);
        }

        if (axis is not null)
        {
            axis.Owner = this;
            axis.InvertedBoundsReported = false;
            AddLogicalChild(axis);
        }
    }

    private CartesianLayoutOptions CreateLayoutOptions(ChartDensity density, CultureInfo culture)
    {
        var full = density == ChartDensity.Full;
        var visible = density != ChartDensity.Minimal;
        var xLabels = full ? (int?)null : CompactXLabelCount;
        var yLabels = full ? (int?)null : CompactYLabelCount;
        effectiveXAxis = XAxis ?? GetDataAxisKind() switch
        {
            ChartAxisKind.Category => defaultCategoryAxis,
            ChartAxisKind.DateTime => defaultTimeAxis,
            _ => defaultNumericAxis
        };
        effectiveSecondaryAxis = SecondaryYAxis ?? (HasSecondarySeries() ? defaultSecondaryYAxis : null);
        var secondary = effectiveSecondaryAxis is not null;
        return new CartesianLayoutOptions
        {
            Orientation = Orientation,
            StackMode = StackMode,
            XAxis = effectiveXAxis.CreateOptions(new AxisContext(visible, full, false, false, xLabels, culture)),
            YAxis = (YAxis ?? defaultYAxis).CreateOptions(new AxisContext(visible, full, HasFilledSeries(ChartAxisTarget.Primary, secondary), true, yLabels, culture)),
            SecondaryYAxis = effectiveSecondaryAxis?.CreateOptions(new AxisContext(visible, full, HasFilledSeries(ChartAxisTarget.Secondary, secondary), false, yLabels, culture)),
            LegendVisible = LegendVisible ?? ((attached.Count >= 2) && full),
            LegendPosition = LegendPosition,
            LegendMaxRows = full ? LegendLayout.MaxRows : 1,
            LegendMode = LegendMode,
            LegendValues = LegendValues,
            LegendWidth = LegendWidth,
            DataLabelsMode = density switch
            {
                ChartDensity.Full => DataLabelsMode,
                ChartDensity.Compact when DataLabelsMode != ChartDataLabelsMode.None => ChartDataLabelsMode.Last,
                _ => ChartDataLabelsMode.None
            },
            LastMarkerOnly = !full,
            GlowEnabled = visible,
            LiveIndicatorExtent = GetLiveIndicatorExtent(),
            Annotations = layoutAnnotations,
            Thresholds = Thresholds
        };
    }

    private double GetLiveIndicatorExtent()
    {
        var extent = 0d;
        foreach (var series in attached)
        {
            if (series is LineSeries { LiveIndicatorVisible: true } line)
            {
                extent = Math.Max(extent, (line.MarkerSize ?? ChartStyle.MarkerSize) / 2 * LiveIndicator.ExtentRatio);
            }
        }

        return extent;
    }

    private void ReportInvertedBounds(CartesianLayoutOptions options)
    {
        ReportInvertedBounds(effectiveXAxis, options.XAxis);
        ReportInvertedBounds(YAxis, options.YAxis);
        ReportInvertedBounds(effectiveSecondaryAxis, options.SecondaryYAxis);
    }

    private bool HasSecondarySeries()
    {
        foreach (var series in attached)
        {
            if (series.YAxis == ChartAxisTarget.Secondary)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasFilledSeries(ChartAxisTarget target, bool secondary)
    {
        foreach (var series in attached)
        {
            if ((series.Kind is not (ChartSeriesKind.Line or ChartSeriesKind.Scatter)) && ((series.YAxis == target) || !secondary))
            {
                return true;
            }
        }

        return false;
    }

    private ChartAxisKind GetDataAxisKind()
    {
        foreach (var series in attached)
        {
            var data = series.Snapshot;
            if (data.Count > 0)
            {
                return data.XKind switch
                {
                    ChartXKind.Category => ChartAxisKind.Category,
                    ChartXKind.Time => ChartAxisKind.DateTime,
                    _ => ChartAxisKind.Numeric
                };
            }
        }

        return ChartAxisKind.Numeric;
    }

    private void ValidateXValues()
    {
        var kind = XAxis?.Kind ?? GetDataAxisKind();
        if (kind == ChartAxisKind.Category)
        {
            return;
        }

        for (var i = 0; i < attached.Count; i++)
        {
            var data = attached[i].Snapshot;
            if ((data.Count > 0) && ((data.XKind == ChartXKind.Time) != (kind == ChartAxisKind.DateTime)))
            {
                throw new InvalidOperationException($"The X values of the series at index {i} do not match the X axis.");
            }
        }
    }
}

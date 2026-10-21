namespace ClamChart.Layout;

internal sealed class SparklineLayout
{
    public const double SmallHeight = 24;

    public const double SmallThickness = 1.5;

    public const double FlatRange = 1;

    public const float MarkerRatio = 1.25f;

    public const float KnockoutRatio = 0.5f;

    private readonly List<ChartThreshold> thresholds = [];

    public ChartRect Bounds { get; private set; }

    public ChartRect Plot { get; private set; }

    public ChartSparklineKind Kind { get; private set; }

    public int Count { get; private set; }

    public SparklineScale Scale { get; private set; } = new(0, 1, 0);

    public float Thickness { get; private set; }

    public float MarkerRadius { get; private set; }

    public float MarkerKnockout { get; private set; }

    public ChartSparklineMarkers Markers { get; private set; }

    public int FirstIndex { get; private set; } = -1;

    public int LastIndex { get; private set; } = -1;

    public int HighIndex { get; private set; } = -1;

    public int LowIndex { get; private set; } = -1;

    public double? Reference { get; private set; }

    public double NormalLow { get; private set; } = Double.NaN;

    public double NormalHigh { get; private set; } = Double.NaN;

    public bool HasNormalRange => !Double.IsNaN(NormalLow) || !Double.IsNaN(NormalHigh);

    public IReadOnlyList<ChartThreshold> Thresholds => thresholds;

    public bool HasInvertedBounds { get; private set; }

    public bool IsBar => Kind is ChartSparklineKind.Bar or ChartSparklineKind.WinLoss;

    public static double GetBase(SparklineScale scale) => Math.Clamp(0, scale.Minimum, scale.Maximum);

    public void Update(double width, double height, Thickness padding, ChartStyle style, SparklineLayoutOptions options, SparklineValues values)
    {
        Bounds = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        Kind = options.Kind;
        Count = values.Count;
        Thickness = (float)(options.Thickness ?? (height < SmallHeight ? SmallThickness : style.LineThickness));
        Markers = IsBar ? ChartSparklineMarkers.None : options.Markers;
        MarkerRadius = Thickness * MarkerRatio;
        MarkerKnockout = Thickness * KnockoutRatio;
        Reference = Kind == ChartSparklineKind.WinLoss ? null : options.ReferenceValue;
        var winLoss = Kind == ChartSparklineKind.WinLoss;
        NormalLow = !winLoss && (options.NormalRangeMinimum is { } normalLow) && Double.IsFinite(normalLow) ? normalLow : Double.NaN;
        NormalHigh = !winLoss && (options.NormalRangeMaximum is { } normalHigh) && Double.IsFinite(normalHigh) ? normalHigh : Double.NaN;
        PrepareThresholds(options.Thresholds);
        var (low, high) = Scan(values);
        var (minimum, maximum) = ResolveRange(options, low, high);
        Scale = new SparklineScale(minimum, maximum, Math.Max(0, Count - 1));
        var inset = IsBar ? 0 : Markers != ChartSparklineMarkers.None ? MarkerRadius + MarkerKnockout : Thickness / 2;
        Plot = new ChartRect(Bounds.X + inset, Bounds.Y + inset, Math.Max(0, Bounds.Width - (inset * 2)), Math.Max(0, Bounds.Height - (inset * 2)));
    }

    public double GetSlot(SparklineScale scale) => Plot.Width / (scale.Last + 1);

    public double ToX(double position, SparklineScale scale)
    {
        if (IsBar)
        {
            return Plot.X + (GetSlot(scale) * (position + 0.5));
        }

        return scale.Last > 0 ? Plot.X + (Plot.Width * position / scale.Last) : Plot.CenterX;
    }

    public double ToY(double value, SparklineScale scale) => Plot.Bottom - ((value - scale.Minimum) / (scale.Maximum - scale.Minimum) * Plot.Height);

    private void PrepareThresholds(IList<ChartThreshold>? source)
    {
        thresholds.Clear();
        if (source is null)
        {
            return;
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < source.Count; i++)
        {
            var item = source[i];
            var index = thresholds.Count;
            while ((index > 0) && (thresholds[index - 1].Value > item.Value))
            {
                index--;
            }

            thresholds.Insert(index, item);
        }
    }

    private (double Low, double High) Scan(SparklineValues values)
    {
        FirstIndex = -1;
        LastIndex = -1;
        HighIndex = -1;
        LowIndex = -1;
        var low = Double.NaN;
        var high = Double.NaN;
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (!Double.IsFinite(value))
            {
                continue;
            }

            if (FirstIndex < 0)
            {
                FirstIndex = i;
            }

            LastIndex = i;
            if (Double.IsNaN(high) || (value > high))
            {
                high = value;
                HighIndex = i;
            }

            if (Double.IsNaN(low) || (value < low))
            {
                low = value;
                LowIndex = i;
            }
        }

        return (low, high);
    }

    private (double Minimum, double Maximum) ResolveRange(SparklineLayoutOptions options, double low, double high)
    {
        if (Kind == ChartSparklineKind.WinLoss)
        {
            HasInvertedBounds = false;
            return (-1, 1);
        }

        if (Reference is { } reference)
        {
            low = Double.IsNaN(low) ? reference : Math.Min(low, reference);
            high = Double.IsNaN(high) ? reference : Math.Max(high, reference);
        }

        if (!Double.IsNaN(NormalLow))
        {
            low = Double.IsNaN(low) ? NormalLow : Math.Min(low, NormalLow);
            high = Double.IsNaN(high) ? NormalLow : Math.Max(high, NormalLow);
        }

        if (!Double.IsNaN(NormalHigh))
        {
            low = Double.IsNaN(low) ? NormalHigh : Math.Min(low, NormalHigh);
            high = Double.IsNaN(high) ? NormalHigh : Math.Max(high, NormalHigh);
        }

        if (Kind == ChartSparklineKind.Bar)
        {
            low = Double.IsNaN(low) ? 0 : Math.Min(low, 0);
            high = Double.IsNaN(high) ? 0 : Math.Max(high, 0);
        }

        HasInvertedBounds = (options.Minimum is { } lower) && (options.Maximum is { } upper) && (lower >= upper);
        var fixedMinimum = HasInvertedBounds ? null : options.Minimum;
        var fixedMaximum = HasInvertedBounds ? null : options.Maximum;
        var minimum = fixedMinimum ?? low;
        var maximum = fixedMaximum ?? high;
        if (Double.IsNaN(minimum))
        {
            minimum = Double.IsNaN(maximum) ? 0 : maximum - FlatRange;
        }

        if (Double.IsNaN(maximum))
        {
            maximum = minimum + FlatRange;
        }

        if (maximum <= minimum)
        {
            if ((fixedMinimum is null) && (fixedMaximum is null))
            {
                minimum -= FlatRange;
                maximum += FlatRange;
            }
            else if (fixedMaximum is null)
            {
                maximum = minimum + FlatRange;
            }
            else
            {
                minimum = maximum - FlatRange;
            }
        }

        return (minimum, maximum);
    }
}

namespace ClamChart;

using ClamChart.Data;
using ClamChart.Layout;

public sealed class BubbleSeries : ScatterSeries
{
    private const double DefaultMinBubbleSize = 8;

    private const double DefaultMaxBubbleSize = 40;

    public static readonly BindableProperty SizeKeyProperty = BindableProperty.Create(nameof(SizeKey), typeof(string), typeof(BubbleSeries), propertyChanged: OnDataChanged);
    public static readonly BindableProperty MinBubbleSizeProperty = BindableProperty.Create(nameof(MinBubbleSize), typeof(double), typeof(BubbleSeries), DefaultMinBubbleSize, validateValue: static (_, value) => ValidateSize(value, nameof(MinBubbleSize)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty MaxBubbleSizeProperty = BindableProperty.Create(nameof(MaxBubbleSize), typeof(double), typeof(BubbleSeries), DefaultMaxBubbleSize, validateValue: static (_, value) => ValidateSize(value, nameof(MaxBubbleSize)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty SizeIncludesZeroProperty = BindableProperty.Create(nameof(SizeIncludesZero), typeof(bool), typeof(BubbleSeries), true, propertyChanged: OnLayoutChanged);

    public string? SizeKey
    {
        get => (string?)GetValue(SizeKeyProperty);
        set => SetValue(SizeKeyProperty, value);
    }

    public double MinBubbleSize
    {
        get => (double)GetValue(MinBubbleSizeProperty);
        set => SetValue(MinBubbleSizeProperty, value);
    }

    public double MaxBubbleSize
    {
        get => (double)GetValue(MaxBubbleSizeProperty);
        set => SetValue(MaxBubbleSizeProperty, value);
    }

    public bool SizeIncludesZero
    {
        get => (bool)GetValue(SizeIncludesZeroProperty);
        set => SetValue(SizeIncludesZeroProperty, value);
    }

    public Func<object, double?>? SizeSelector
    {
        get;
        set
        {
            field = value;
            Owner?.InvalidateData();
        }
    }

    internal override CartesianSeries CreateLayoutSeries() =>
        base.CreateLayoutSeries() with
        {
            IsBubble = true,
            MinBubbleSize = MinBubbleSize,
            MaxBubbleSize = MaxBubbleSize,
            SizeIncludesZero = SizeIncludesZero
        };

    internal override ChartValueOptions CreateValueOptions(IChartAccessorProvider? accessors, string? xKey) =>
        base.CreateValueOptions(accessors, xKey) with
        {
            SizeKey = SizeKey,
            SizeSelector = SizeSelector
        };
}

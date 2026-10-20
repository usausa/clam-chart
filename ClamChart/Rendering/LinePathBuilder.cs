namespace ClamChart.Rendering;

using SkiaSharp;

internal static class LinePathBuilder
{
    private const float MaxTangentRatio = 9;

    public static void ComputeTangents(ReadOnlySpan<float> along, ReadOnlySpan<float> across, Span<float> tangents)
    {
        var count = along.Length;
        if (count < 2)
        {
            if (count == 1)
            {
                tangents[0] = 0;
            }

            return;
        }

        for (var i = 0; i < count; i++)
        {
            if (i == 0)
            {
                tangents[i] = GetSlope(along, across, 0);
            }
            else if (i == count - 1)
            {
                tangents[i] = GetSlope(along, across, i - 1);
            }
            else
            {
                var before = GetSlope(along, across, i - 1);
                var after = GetSlope(along, across, i);
                tangents[i] = before * after <= 0 ? 0 : (before + after) / 2;
            }
        }

        for (var i = 0; i < count - 1; i++)
        {
            var slope = GetSlope(along, across, i);
            if (slope == 0)
            {
                tangents[i] = 0;
                tangents[i + 1] = 0;
                continue;
            }

            var a = tangents[i] / slope;
            var b = tangents[i + 1] / slope;
            var ratio = (a * a) + (b * b);
            if (ratio > MaxTangentRatio)
            {
                var scale = 3 / MathF.Sqrt(ratio);
                tangents[i] = scale * a * slope;
                tangents[i + 1] = scale * b * slope;
            }
        }
    }

    public static void AddLine(SKPathBuilder path, ReadOnlySpan<float> along, ReadOnlySpan<float> across, ReadOnlySpan<float> tangents, ChartLineInterpolation interpolation, bool horizontal)
    {
        path.MoveTo(ToPoint(along[0], across[0], horizontal));
        if (along.Length == 1)
        {
            path.LineTo(ToPoint(along[0], across[0], horizontal));
            return;
        }

        for (var i = 1; i < along.Length; i++)
        {
            switch (interpolation)
            {
                case ChartLineInterpolation.Step:
                    path.LineTo(ToPoint(along[i], across[i - 1], horizontal));
                    path.LineTo(ToPoint(along[i], across[i], horizontal));
                    break;
                case ChartLineInterpolation.Smooth:
                    var third = (along[i] - along[i - 1]) / 3;
                    path.CubicTo(
                        ToPoint(along[i - 1] + third, across[i - 1] + (tangents[i - 1] * third), horizontal),
                        ToPoint(along[i] - third, across[i] - (tangents[i] * third), horizontal),
                        ToPoint(along[i], across[i], horizontal));
                    break;
                default:
                    path.LineTo(ToPoint(along[i], across[i], horizontal));
                    break;
            }
        }
    }

    public static void AddReversedLine(SKPathBuilder path, ReadOnlySpan<float> along, ReadOnlySpan<float> across, ReadOnlySpan<float> tangents, ChartLineInterpolation interpolation, bool horizontal)
    {
        var last = along.Length - 1;
        path.LineTo(ToPoint(along[last], across[last], horizontal));
        for (var i = last; i > 0; i--)
        {
            switch (interpolation)
            {
                case ChartLineInterpolation.Step:
                    path.LineTo(ToPoint(along[i], across[i - 1], horizontal));
                    path.LineTo(ToPoint(along[i - 1], across[i - 1], horizontal));
                    break;
                case ChartLineInterpolation.Smooth:
                    var third = (along[i] - along[i - 1]) / 3;
                    path.CubicTo(
                        ToPoint(along[i] - third, across[i] - (tangents[i] * third), horizontal),
                        ToPoint(along[i - 1] + third, across[i - 1] + (tangents[i - 1] * third), horizontal),
                        ToPoint(along[i - 1], across[i - 1], horizontal));
                    break;
                default:
                    path.LineTo(ToPoint(along[i - 1], across[i - 1], horizontal));
                    break;
            }
        }
    }

    public static SKPoint ToPoint(float along, float across, bool horizontal) => horizontal ? new SKPoint(across, along) : new SKPoint(along, across);

    private static float GetSlope(ReadOnlySpan<float> along, ReadOnlySpan<float> across, int index)
    {
        var width = along[index + 1] - along[index];
        return width != 0 ? (across[index + 1] - across[index]) / width : 0;
    }
}

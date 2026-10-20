namespace ClamChart.Rendering;

using System.Globalization;

using SkiaSharp;

internal sealed class ChartSpanText
{
    private readonly Dictionary<string, string> elements = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> lookup;

    public int ElementCount => elements.Count;

    public ChartSpanText()
    {
        lookup = elements.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    public float Measure(ChartTextPainter painter, ReadOnlySpan<char> text, float size)
    {
        var width = 0f;
        var index = 0;
        while (index < text.Length)
        {
            var length = StringInfo.GetNextTextElementLength(text[index..]);
            width += painter.Create(Intern(text.Slice(index, length)), size).Width;
            index += length;
        }

        return width;
    }

    public void Draw(SKCanvas canvas, ChartTextPainter painter, ReadOnlySpan<char> text, float size, float x, float baseline, SKColor color, SKColor haloColor, float haloWidth)
    {
        if ((haloWidth > 0) && (haloColor.Alpha > 0))
        {
            DrawPass(canvas, painter, text, size, x, baseline, haloColor, haloWidth);
        }

        DrawPass(canvas, painter, text, size, x, baseline, color, 0);
    }

    private void DrawPass(SKCanvas canvas, ChartTextPainter painter, ReadOnlySpan<char> text, float size, float x, float baseline, SKColor color, float haloWidth)
    {
        var index = 0;
        while (index < text.Length)
        {
            var length = StringInfo.GetNextTextElementLength(text[index..]);
            var drawable = painter.Create(Intern(text.Slice(index, length)), size);
            if (haloWidth > 0)
            {
                painter.DrawHalo(canvas, drawable, x, baseline, SKTextAlign.Left, color, haloWidth);
            }
            else
            {
                painter.DrawFill(canvas, drawable, x, baseline, SKTextAlign.Left, color);
            }

            x += drawable.Width;
            index += length;
        }
    }

    private string Intern(ReadOnlySpan<char> element)
    {
        if (!lookup.TryGetValue(element, out var value))
        {
            value = element.ToString();
            elements.Add(value, value);
        }

        return value;
    }
}

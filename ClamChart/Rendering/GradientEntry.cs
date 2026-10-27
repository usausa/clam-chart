namespace ClamChart.Rendering;

using SkiaSharp;

internal sealed class GradientEntry
{
    public SKShader? Shader { get; set; }

    public SKColor Color { get; set; }

    public float From { get; set; }

    public float To { get; set; }

    public bool Horizontal { get; set; }

    public byte Tail { get; set; }

    public int Frame { get; set; }

    public SKShader Update(SKColor color, float from, float to, bool horizontal, byte tail = 0)
    {
        if ((Shader is null) || (Color != color) || !From.Equals(from) || !To.Equals(to) || (Horizontal != horizontal) || (Tail != tail))
        {
            Shader?.Dispose();
            Color = color;
            From = from;
            To = to;
            Horizontal = horizontal;
            Tail = tail;
            Shader = SKShader.CreateLinearGradient(
                LinePathBuilder.ToPoint(0, from, horizontal),
                LinePathBuilder.ToPoint(0, to, horizontal),
                [color, color.WithAlpha(tail)],
                SKShaderTileMode.Clamp);
        }

        return Shader;
    }
}

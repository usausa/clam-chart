namespace ClamChart.Layout;

internal readonly record struct ChartRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;

    public double Bottom => Y + Height;

    public double CenterX => X + (Width / 2);

    public double CenterY => Y + (Height / 2);

    public bool IsEmpty => (Width <= 0) || (Height <= 0);

    public bool Contains(double x, double y) => !IsEmpty && (x >= X) && (x < Right) && (y >= Y) && (y < Bottom);
}

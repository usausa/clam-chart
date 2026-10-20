namespace ClamChart;

public static class ChartPalettes
{
    public static IReadOnlyList<Color> DefaultLight { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#2A78D6"),
        Color.FromArgb("#EB6834"),
        Color.FromArgb("#1BAF7A"),
        Color.FromArgb("#EDA100"),
        Color.FromArgb("#E87BA4"),
        Color.FromArgb("#008300"),
        Color.FromArgb("#6250D6"),
        Color.FromArgb("#E34948")
    ]);

    public static IReadOnlyList<Color> DefaultDark { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#3987E5"),
        Color.FromArgb("#D95926"),
        Color.FromArgb("#199E70"),
        Color.FromArgb("#C98500"),
        Color.FromArgb("#D55181"),
        Color.FromArgb("#008300"),
        Color.FromArgb("#9085E9"),
        Color.FromArgb("#E66767")
    ]);
}

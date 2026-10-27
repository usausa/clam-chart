namespace Example.Models;

public static class AppPalettes
{
    public static IReadOnlyList<Color> Material { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#42A5F5"),
        Color.FromArgb("#66BB6A"),
        Color.FromArgb("#FFCA28"),
        Color.FromArgb("#EC407A"),
        Color.FromArgb("#26C6DA"),
        Color.FromArgb("#7E57C2"),
        Color.FromArgb("#FF7043"),
        Color.FromArgb("#26A69A")
    ]);

    public static IReadOnlyList<Color> BlueScale { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#1A3274D9"),
        Color.FromArgb("#3274D9")
    ]);
}

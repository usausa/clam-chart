namespace Example.Models;

public static class AppPalettes
{
    public static IReadOnlyList<Color> Material { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#42A5F5"),
        Color.FromArgb("#66BB6A"),
        Color.FromArgb("#FFCA28"),
        Color.FromArgb("#EC407A"),
        Color.FromArgb("#26C6DA")
    ]);
}

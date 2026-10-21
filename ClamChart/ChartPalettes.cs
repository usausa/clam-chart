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

    public static IReadOnlyList<Color> GrafanaLight { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#56A64B"),
        Color.FromArgb("#E0B400"),
        Color.FromArgb("#3274D9"),
        Color.FromArgb("#FF780A"),
        Color.FromArgb("#E02F44"),
        Color.FromArgb("#A352CC")
    ]);

    public static IReadOnlyList<Color> GrafanaDark { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#73BF69"),
        Color.FromArgb("#F2CC0C"),
        Color.FromArgb("#5794F2"),
        Color.FromArgb("#FF9830"),
        Color.FromArgb("#F2495C"),
        Color.FromArgb("#B877D9")
    ]);

    public static IReadOnlyList<Color> RedShadesLight { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#E02F44"),
        Color.FromArgb("#EF3E4E"),
        Color.FromArgb("#D21F39"),
        Color.FromArgb("#FE4D58"),
        Color.FromArgb("#B81B37"),
        Color.FromArgb("#FF5C62"),
        Color.FromArgb("#9E1833"),
        Color.FromArgb("#FF6A6B"),
        Color.FromArgb("#84142E"),
        Color.FromArgb("#FF7D79"),
        Color.FromArgb("#6A1027")
    ]);

    public static IReadOnlyList<Color> RedShadesDark { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#F2495C"),
        Color.FromArgb("#FF5866"),
        Color.FromArgb("#F02D49"),
        Color.FromArgb("#FF6771"),
        Color.FromArgb("#EE1136"),
        Color.FromArgb("#FF767B"),
        Color.FromArgb("#D30F35"),
        Color.FromArgb("#FF8484"),
        Color.FromArgb("#B70D33"),
        Color.FromArgb("#FF9793"),
        Color.FromArgb("#9B0B2F")
    ]);

    public static IReadOnlyList<Color> OrangeShadesLight { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#FF780A"),
        Color.FromArgb("#FF8E19"),
        Color.FromArgb("#EB6300"),
        Color.FromArgb("#FFA428"),
        Color.FromArgb("#CD5100"),
        Color.FromArgb("#FFB937"),
        Color.FromArgb("#B04000"),
        Color.FromArgb("#FFCE45"),
        Color.FromArgb("#923100"),
        Color.FromArgb("#FFE454"),
        Color.FromArgb("#742400")
    ]);

    public static IReadOnlyList<Color> OrangeShadesDark { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#FF9830"),
        Color.FromArgb("#FFAD3F"),
        Color.FromArgb("#FF8312"),
        Color.FromArgb("#FFC14E"),
        Color.FromArgb("#F36D00"),
        Color.FromArgb("#FFD65D"),
        Color.FromArgb("#D65A00"),
        Color.FromArgb("#FFEA6B"),
        Color.FromArgb("#B84800"),
        Color.FromArgb("#FFFF7A"),
        Color.FromArgb("#9A3800")
    ]);

    public static IReadOnlyList<Color> YellowShadesLight { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#F2CC0C"),
        Color.FromArgb("#FFE11B"),
        Color.FromArgb("#D6AE0B"),
        Color.FromArgb("#FFF72A"),
        Color.FromArgb("#B99209"),
        Color.FromArgb("#FFFF39"),
        Color.FromArgb("#9D7808"),
        Color.FromArgb("#FFFF47"),
        Color.FromArgb("#815F06"),
        Color.FromArgb("#FFFF56"),
        Color.FromArgb("#644705")
    ]);

    public static IReadOnlyList<Color> YellowShadesDark { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#FADE2A"),
        Color.FromArgb("#FFF339"),
        Color.FromArgb("#F9D30D"),
        Color.FromArgb("#FFFF48"),
        Color.FromArgb("#E3B905"),
        Color.FromArgb("#FFFF57"),
        Color.FromArgb("#C69C05"),
        Color.FromArgb("#FFFF65"),
        Color.FromArgb("#A98004"),
        Color.FromArgb("#FFFF74"),
        Color.FromArgb("#8C6703")
    ]);

    public static IReadOnlyList<Color> GreenShadesLight { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#56A64B"),
        Color.FromArgb("#62B55A"),
        Color.FromArgb("#4E9142"),
        Color.FromArgb("#6FC469"),
        Color.FromArgb("#457D38"),
        Color.FromArgb("#7BD378"),
        Color.FromArgb("#3B692F"),
        Color.FromArgb("#87E186"),
        Color.FromArgb("#315426"),
        Color.FromArgb("#95F097"),
        Color.FromArgb("#26401D")
    ]);

    public static IReadOnlyList<Color> GreenShadesDark { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#73BF69"),
        Color.FromArgb("#80CE78"),
        Color.FromArgb("#62B654"),
        Color.FromArgb("#8CDD87"),
        Color.FromArgb("#57A647"),
        Color.FromArgb("#99EC96"),
        Color.FromArgb("#4E913E"),
        Color.FromArgb("#A4FAA4"),
        Color.FromArgb("#457C35"),
        Color.FromArgb("#B3FFB5"),
        Color.FromArgb("#3B672C")
    ]);

    public static IReadOnlyList<Color> BlueShadesLight { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#3274D9"),
        Color.FromArgb("#417EE8"),
        Color.FromArgb("#256AC8"),
        Color.FromArgb("#5089F7"),
        Color.FromArgb("#2061AF"),
        Color.FromArgb("#5F93FF"),
        Color.FromArgb("#1C5696"),
        Color.FromArgb("#6D9CFF"),
        Color.FromArgb("#174B7D"),
        Color.FromArgb("#7CA7FF"),
        Color.FromArgb("#123E64")
    ]);

    public static IReadOnlyList<Color> BlueShadesDark { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#5794F2"),
        Color.FromArgb("#669FFF"),
        Color.FromArgb("#3B87F0"),
        Color.FromArgb("#75A9FF"),
        Color.FromArgb("#207CEE"),
        Color.FromArgb("#84B4FF"),
        Color.FromArgb("#1173DE"),
        Color.FromArgb("#92BEFF"),
        Color.FromArgb("#0F6AC3"),
        Color.FromArgb("#A1C8FF"),
        Color.FromArgb("#0D5FA7")
    ]);

    public static IReadOnlyList<Color> PurpleShadesLight { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#A352CC"),
        Color.FromArgb("#B561DB"),
        Color.FromArgb("#933BC5"),
        Color.FromArgb("#C870EA"),
        Color.FromArgb("#7F34AF"),
        Color.FromArgb("#DA7FF9"),
        Color.FromArgb("#6B2D98"),
        Color.FromArgb("#EC8DFF"),
        Color.FromArgb("#582681"),
        Color.FromArgb("#FE9CFF"),
        Color.FromArgb("#461F6A")
    ]);

    public static IReadOnlyList<Color> PurpleShadesDark { get; } = Array.AsReadOnly<Color>(
    [
        Color.FromArgb("#B877D9"),
        Color.FromArgb("#CA86E8"),
        Color.FromArgb("#A960D2"),
        Color.FromArgb("#DB95F7"),
        Color.FromArgb("#9848CC"),
        Color.FromArgb("#EDA4FF"),
        Color.FromArgb("#8636C1"),
        Color.FromArgb("#FEB2FF"),
        Color.FromArgb("#732FAA"),
        Color.FromArgb("#FFC1FF"),
        Color.FromArgb("#602992")
    ]);
}

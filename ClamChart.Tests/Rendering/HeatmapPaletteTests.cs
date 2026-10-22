namespace ClamChart.Tests.Rendering;

using SkiaSharp;

public sealed class HeatmapPaletteTests
{
    private static readonly string[] GrafanaOranges =
    [
        "#FFF5EB", "#FFF3E7", "#FFF1E4", "#FFEFE0", "#FEEDDC", "#FEEBD8", "#FEE9D4", "#FEE7CF",
        "#FEE5CB", "#FEE2C6", "#FEE0C1", "#FEDDBC", "#FEDAB6", "#FDD7B1", "#FDD4AB", "#FDD1A5",
        "#FDCD9F", "#FDC998", "#FDC692", "#FDC28B", "#FDBD84", "#FDB97E", "#FDB577", "#FDB170",
        "#FDAD6A", "#FDA864", "#FDA45D", "#FDA057", "#FD9C51", "#FC974B", "#FC9345", "#FB8F40",
        "#FB8A3A", "#FA8635", "#F8812F", "#F77D2A", "#F67825", "#F47421", "#F2701C", "#F06B18",
        "#EE6715", "#EB6211", "#E85E0E", "#E55A0C", "#E25609", "#DE5207", "#DA4F06", "#D64B05",
        "#D14804", "#CC4503", "#C64203", "#C04002", "#BA3D02", "#B43B02", "#AF3903", "#A93703",
        "#A33503", "#9E3303", "#983103", "#932F03", "#8E2D04", "#892B04", "#842904", "#7F2704"
    ];

    [Fact]
    public void OrangesMatchThePaletteOfGrafana()
    {
        // Arrange
        var light = new SKColor[HeatmapPalette.Steps];
        var dark = new SKColor[HeatmapPalette.Steps];

        // Act
        HeatmapPalette.Build(ChartPalettes.OrangesLight, light);
        HeatmapPalette.Build(ChartPalettes.OrangesDark, dark);

        // Assert
        Assert.Equal(GrafanaOranges, light.Select(ToHex));
        Assert.Equal(GrafanaOranges.Reverse(), dark.Select(ToHex));
        Assert.All(light, static x => Assert.Equal(255, x.Alpha));
    }

    [Fact]
    public void TwoColorsMakeASmoothRamp()
    {
        // Arrange
        var palette = new SKColor[3];

        // Act
        HeatmapPalette.Build([Colors.Black, Colors.White], palette);

        // Assert
        Assert.Equal(new SKColor(0, 0, 0), palette[0]);
        Assert.Equal(new SKColor(128, 128, 128), palette[1]);
        Assert.Equal(new SKColor(255, 255, 255), palette[2]);
    }

    [Fact]
    public void ValuesAreQuantizedLikeGrafana()
    {
        // Act & Assert
        Assert.Equal(0, HeatmapPalette.GetIndex(10, 10, 20, 64));
        Assert.Equal(32, HeatmapPalette.GetIndex(15, 10, 20, 64));
        Assert.Equal(63, HeatmapPalette.GetIndex(20, 10, 20, 64));
        Assert.Equal(63, HeatmapPalette.GetIndex(19.999, 10, 20, 64));
        Assert.Equal(0, HeatmapPalette.GetIndex(-5, 10, 20, 64));
        Assert.Equal(63, HeatmapPalette.GetIndex(25, 10, 20, 64));
        Assert.Equal(0, HeatmapPalette.GetIndex(10, 10, 10, 64));
    }

    private static string ToHex(SKColor color) => $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
}

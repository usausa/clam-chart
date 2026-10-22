namespace ClamChart.Tests;

public sealed class ChartStyleTests
{
    [Fact]
    public void LightStyleHasTheDocumentedDefaults()
    {
        // Arrange
        var style = ChartStyles.Light;

        // Act & Assert
        Assert.Equal("sans-serif", style.FontFamily);
        Assert.Equal(Color.FromArgb("#0B0B0B"), style.PrimaryTextColor);
        Assert.Equal(Color.FromArgb("#E1E0D9"), style.GridLineColor);
        Assert.Equal(0d, style.GridLineThickness);
        Assert.Equal(Color.FromArgb("#F0FCFCFB"), style.TooltipBackground);
        Assert.Null(style.TrackColor);
        Assert.Equal(0.35, style.DimmedOpacity);
        Assert.Equal(0.35, style.EmphasisDimmedOpacity);
        Assert.Equal(0.1, style.AreaFillOpacity);
        Assert.Equal(24d, style.MaxBarWidth);
        Assert.Equal(0d, style.BarCornerRadius);
        Assert.Equal(0d, style.TooltipCornerRadius);
        Assert.False(style.IsDark);
        Assert.Equal(Color.FromArgb("#56A64B"), style.GoodColor);
        Assert.Equal(Color.FromArgb("#F2CC0C"), style.WarningColor);
        Assert.Equal(Color.FromArgb("#FF780A"), style.SeriousColor);
        Assert.Equal(Color.FromArgb("#E02F44"), style.CriticalColor);
        Assert.Equal(Color.FromArgb("#56A64B"), style.PositiveTextColor);
        Assert.Equal(Color.FromArgb("#E02F44"), style.NegativeTextColor);
        Assert.Equal(new Size(320, 200), style.CompactSize);
        Assert.Equal(new Size(160, 96), style.MinimalSize);
        Assert.Same(ChartPalettes.DefaultLight, style.Palette);
        Assert.Same(ChartPalettes.OrangesLight, style.HeatmapColorScale);
    }

    [Fact]
    public void DarkStyleChangesOnlyTheDocumentedValues()
    {
        // Arrange
        var light = ChartStyles.Light;
        var dark = ChartStyles.Dark;

        // Act & Assert
        Assert.Equal(Color.FromArgb("#F0EFEC"), dark.PrimaryTextColor);
        Assert.Equal(Color.FromArgb("#2C2C2A"), dark.GridLineColor);
        Assert.Equal(Color.FromArgb("#F01F1F1D"), dark.TooltipBackground);
        Assert.True(dark.IsDark);
        Assert.Equal(Color.FromArgb("#73BF69"), dark.GoodColor);
        Assert.Equal(Color.FromArgb("#FADE2A"), dark.WarningColor);
        Assert.Equal(Color.FromArgb("#FF9830"), dark.SeriousColor);
        Assert.Equal(Color.FromArgb("#F2495C"), dark.CriticalColor);
        Assert.Equal(Color.FromArgb("#73BF69"), dark.PositiveTextColor);
        Assert.Equal(Color.FromArgb("#F2495C"), dark.NegativeTextColor);
        Assert.Same(ChartPalettes.DefaultDark, dark.Palette);
        Assert.Same(ChartPalettes.OrangesDark, dark.HeatmapColorScale);
        Assert.Equal(0.5, dark.EmphasisDimmedOpacity);
        Assert.Equal(light.DimmedOpacity, dark.DimmedOpacity);
        Assert.Equal(light.MutedTextColor, dark.MutedTextColor);
        Assert.Equal(light.AxisLabelFontSize, dark.AxisLabelFontSize);
        Assert.Equal(light.CompactSize, dark.CompactSize);
    }

    [Fact]
    public void PalettesHaveEightColorsInAFixedOrder()
    {
        // Act & Assert
        Assert.Equal(8, ChartPalettes.DefaultLight.Count);
        Assert.Equal(8, ChartPalettes.DefaultDark.Count);
        Assert.Equal(Color.FromArgb("#2A78D6"), ChartPalettes.DefaultLight[0]);
        Assert.Equal(Color.FromArgb("#E34948"), ChartPalettes.DefaultLight[7]);
        Assert.Equal(Color.FromArgb("#3987E5"), ChartPalettes.DefaultDark[0]);
        Assert.Equal(Color.FromArgb("#E66767"), ChartPalettes.DefaultDark[7]);
        Assert.IsNotType<Color[]>(ChartPalettes.DefaultLight);
        Assert.Equal(6, ChartPalettes.GrafanaLight.Count);
        Assert.Equal(6, ChartPalettes.GrafanaDark.Count);
        Assert.Equal(Color.FromArgb("#56A64B"), ChartPalettes.GrafanaLight[0]);
        Assert.Equal(Color.FromArgb("#E0B400"), ChartPalettes.GrafanaLight[1]);
        Assert.Equal(Color.FromArgb("#A352CC"), ChartPalettes.GrafanaLight[5]);
        Assert.Equal(Color.FromArgb("#73BF69"), ChartPalettes.GrafanaDark[0]);
        Assert.Equal(Color.FromArgb("#F2CC0C"), ChartPalettes.GrafanaDark[1]);
        Assert.Equal(Color.FromArgb("#B877D9"), ChartPalettes.GrafanaDark[5]);
        Assert.IsNotType<Color[]>(ChartPalettes.GrafanaLight);
    }

    [Fact]
    public void OverlayPresetsHideTheGridAndOutlineTheText()
    {
        // Arrange
        var light = ChartStyles.OverlayLight;
        var dark = ChartStyles.OverlayDark;

        // Act & Assert
        light.Validate();
        dark.Validate();
        Assert.Equal(Colors.Transparent, light.GridLineColor);
        Assert.Equal(Colors.Transparent, light.AxisLineColor);
        Assert.Equal(Color.FromArgb("#CCFFFFFF"), light.TextHaloColor);
        Assert.Equal(1.5, light.TextHaloWidth);
        Assert.Equal(0.06, light.AreaFillOpacity);
        Assert.Same(ChartPalettes.DefaultLight, light.Palette);
        Assert.False(light.IsDark);
        Assert.Equal(Colors.Transparent, dark.GridLineColor);
        Assert.Equal(Colors.Transparent, dark.AxisLineColor);
        Assert.Equal(Color.FromArgb("#CC000000"), dark.TextHaloColor);
        Assert.Equal(1.5, dark.TextHaloWidth);
        Assert.Equal(0.08, dark.AreaFillOpacity);
        Assert.Same(ChartPalettes.DefaultDark, dark.Palette);
        Assert.True(dark.IsDark);
        Assert.Equal(ChartStyles.Dark.PrimaryTextColor, dark.PrimaryTextColor);
    }

    [Fact]
    public void ShadesStartWithThePrimaryColorAndAlternateLighterAndDarker()
    {
        // Arrange
        (IReadOnlyList<Color> Palette, string Primary)[] shades =
        [
            (ChartPalettes.RedShadesLight, "#E02F44"),
            (ChartPalettes.RedShadesDark, "#F2495C"),
            (ChartPalettes.OrangeShadesLight, "#FF780A"),
            (ChartPalettes.OrangeShadesDark, "#FF9830"),
            (ChartPalettes.YellowShadesLight, "#F2CC0C"),
            (ChartPalettes.YellowShadesDark, "#FADE2A"),
            (ChartPalettes.GreenShadesLight, "#56A64B"),
            (ChartPalettes.GreenShadesDark, "#73BF69"),
            (ChartPalettes.BlueShadesLight, "#3274D9"),
            (ChartPalettes.BlueShadesDark, "#5794F2"),
            (ChartPalettes.PurpleShadesLight, "#A352CC"),
            (ChartPalettes.PurpleShadesDark, "#B877D9")
        ];

        // Act & Assert
        foreach (var (palette, primary) in shades)
        {
            Assert.Equal(11, palette.Count);
            Assert.Equal(Color.FromArgb(primary), palette[0]);
            Assert.IsNotType<Color[]>(palette);
            var luminosity = palette[0].GetLuminosity();
            for (var i = 1; i < palette.Count; i += 2)
            {
                Assert.True(palette[i].GetLuminosity() > luminosity);
                Assert.True(palette[i + 1].GetLuminosity() < luminosity);
            }
        }
    }

    [Fact]
    public void ShadesMatchTheCalculationOfGrafana()
    {
        // Act & Assert
        Assert.Equal(
            ["#3274D9", "#417EE8", "#256AC8", "#5089F7", "#2061AF", "#5F93FF", "#1C5696", "#6D9CFF", "#174B7D", "#7CA7FF", "#123E64"],
            ChartPalettes.BlueShadesLight.Select(static x => x.ToRgbaHex()[..7]));
        Assert.Equal(
            ["#5794F2", "#669FFF", "#3B87F0", "#75A9FF", "#207CEE", "#84B4FF", "#1173DE", "#92BEFF", "#0F6AC3", "#A1C8FF", "#0D5FA7"],
            ChartPalettes.BlueShadesDark.Select(static x => x.ToRgbaHex()[..7]));
        Assert.Equal(Color.FromArgb("#FFFF74"), ChartPalettes.YellowShadesDark[9]);
        Assert.Equal(Color.FromArgb("#6A1027"), ChartPalettes.RedShadesLight[10]);
    }

    [Fact]
    public void OrangesAreTheSchemeOfGrafanaAndReversedForDark()
    {
        // Act & Assert
        Assert.Equal(
            ["#FFF5EB", "#FEE6CE", "#FDD0A2", "#FDAE6B", "#FD8D3C", "#F16913", "#D94801", "#A63603", "#7F2704"],
            ChartPalettes.OrangesLight.Select(static x => x.ToRgbaHex()[..7]));
        Assert.Equal(ChartPalettes.OrangesLight.Reverse(), ChartPalettes.OrangesDark);
    }

    [Fact]
    public void WithCreatesAnIndependentCopy()
    {
        // Act
        var style = ChartStyles.Light with { LineThickness = 3 };

        // Assert
        Assert.Equal(3d, style.LineThickness);
        Assert.Equal(2d, ChartStyles.Light.LineThickness);
        Assert.NotSame(ChartStyles.Light, style);
    }

    [Fact]
    public void PresetsAreValid()
    {
        // Act & Assert
        Assert.Null(Record.Exception(ChartStyles.Light.Validate));
        Assert.Null(Record.Exception(ChartStyles.Dark.Validate));
    }

    [Fact]
    public void ValidationRejectsInvalidValues()
    {
        // Arrange
        ChartStyle[] styles =
        [
            ChartStyles.Light with { AxisLabelFontSize = 0 },
            ChartStyles.Light with { TooltipFontSize = Double.NaN },
            ChartStyles.Light with { LineThickness = -1 },
            ChartStyles.Light with { MarkerSize = Double.PositiveInfinity },
            ChartStyles.Light with { TextHaloWidth = -0.5 },
            ChartStyles.Light with { GridLineThickness = Double.NaN },
            ChartStyles.Light with { SegmentGap = -1 },
            ChartStyles.Light with { DimmedOpacity = 1.5 },
            ChartStyles.Light with { EmphasisDimmedOpacity = -0.1 },
            ChartStyles.Light with { AreaFillOpacity = Double.NaN },
            ChartStyles.Light with { CompactSize = new Size(-1, 200) },
            ChartStyles.Light with { MinimalSize = new Size(400, 96) },
            ChartStyles.Light with { HeatmapColorScale = [Colors.Red] }
        ];

        // Act & Assert
        foreach (var style in styles)
        {
            Assert.Throws<ArgumentException>(style.Validate);
        }
    }
}

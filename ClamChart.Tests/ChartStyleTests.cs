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
            ChartStyles.Light with { AreaFillOpacity = Double.NaN },
            ChartStyles.Light with { CompactSize = new Size(-1, 200) },
            ChartStyles.Light with { MinimalSize = new Size(400, 96) }
        ];

        // Act & Assert
        foreach (var style in styles)
        {
            Assert.Throws<ArgumentException>(style.Validate);
        }
    }
}

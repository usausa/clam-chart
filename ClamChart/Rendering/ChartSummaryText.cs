namespace ClamChart.Rendering;

using System.Globalization;
using System.Text;

internal sealed class ChartSummaryText
{
    private readonly StringBuilder builder = new();
    private bool itemWritten;

    public bool IsJapanese { get; }

    public CultureInfo Culture { get; }

    public ChartSummaryText(CultureInfo language, CultureInfo culture)
    {
        IsJapanese = String.Equals(language.TwoLetterISOLanguageName, "ja", StringComparison.Ordinal);
        Culture = culture;
    }

    public string SeriesName(int number) => (IsJapanese ? "系列 " : "Series ") + number.ToString(Culture);

    public void BeginGroup(string? name)
    {
        if (builder.Length > 0)
        {
            builder.Append(IsJapanese ? "。" : "; ");
        }

        itemWritten = false;
        if (!String.IsNullOrEmpty(name))
        {
            builder.Append(name);
            builder.Append(IsJapanese ? "、" : ": ");
        }
    }

    public void AddCount(int count, bool items)
    {
        var number = count.ToString(Culture);
        if (IsJapanese)
        {
            AddText(number + (items ? " 項目" : " 点"));
        }
        else if (items)
        {
            AddText(number + (count == 1 ? " item" : " items"));
        }
        else
        {
            AddText(number + (count == 1 ? " point" : " points"));
        }
    }

    public void AddValue(ChartSummaryWord word, string value, string? label = null)
    {
        var name = GetWord(word);
        if (String.IsNullOrEmpty(label))
        {
            AddText(name + " " + value);
        }
        else
        {
            AddText(IsJapanese ? name + " " + value + "（" + label + "）" : name + " " + value + " (" + label + ")");
        }
    }

    public void AddText(string text)
    {
        if (itemWritten)
        {
            builder.Append(IsJapanese ? "、" : ", ");
        }

        builder.Append(text);
        itemWritten = true;
    }

    public string? Build() => builder.Length > 0 ? builder.ToString() : null;

    private string GetWord(ChartSummaryWord word) => word switch
    {
        ChartSummaryWord.Maximum => IsJapanese ? "最大" : "max",
        ChartSummaryWord.Minimum => IsJapanese ? "最小" : "min",
        ChartSummaryWord.Last => IsJapanese ? "最新" : "last",
        _ => IsJapanese ? "合計" : "total"
    };
}

namespace ClamChart.Rendering;

internal readonly record struct CartesianSelection(bool Active, bool Category, int Series, int Index, double X)
{
    public bool DimsSeries(int series) => Active && !Category && (series != Series);

    public bool DimsPoint(int series, int index, double x) => Active && (Category ? !x.Equals(X) : (series != Series) || (index != Index));
}

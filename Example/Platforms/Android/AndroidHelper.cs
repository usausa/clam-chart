#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Example;

public static class AndroidHelper
{
    public static void MoveTaskToBack() =>
        Platform.CurrentActivity?.MoveTaskToBack(true);
}

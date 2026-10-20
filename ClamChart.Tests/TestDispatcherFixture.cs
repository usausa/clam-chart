namespace ClamChart.Tests;

using Microsoft.Maui.Dispatching;

public sealed class TestDispatcherFixture : IDisposable
{
    public TestDispatcherFixture()
    {
        DispatcherProvider.SetCurrent(TestDispatcherProvider.Instance);
    }

    public void Dispose()
    {
        DispatcherProvider.SetCurrent(null);
    }
}

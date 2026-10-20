namespace ClamChart.Tests;

using System.Diagnostics;

internal sealed class TraceCapture : TraceListener
{
    private readonly List<string> messages = [];

    public TraceCapture()
    {
        Trace.Listeners.Add(this);
    }

    public int Count(string text)
    {
        lock (messages)
        {
            return messages.Count(x => x.Contains(text, StringComparison.Ordinal));
        }
    }

    public override void Write(string? message)
    {
    }

    public override void WriteLine(string? message)
    {
        if (message is not null)
        {
            lock (messages)
            {
                messages.Add(message);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        Trace.Listeners.Remove(this);
        base.Dispose(disposing);
    }
}

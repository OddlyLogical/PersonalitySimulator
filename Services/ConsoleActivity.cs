using System.Diagnostics;

namespace PersonalitySimulator.Services;

public static class ConsoleActivity
{
    public static async Task<T> RunAsync<T>(Func<Task<T>> operation, string label = "Thinking",
        TextWriter? output = null, bool? animate = null)
    {
        output ??= Console.Out;
        var interactive = animate ?? (ReferenceEquals(output, Console.Out) && !Console.IsOutputRedirected);
        if (!interactive)
        {
            TryWrite(output, label + "..." + Environment.NewLine);
            return await operation();
        }

        var clock = Stopwatch.StartNew();
        const string frames = "|/-\\";
        int frame = 0, width = 0;
        try
        {
            var pending = operation();
            while (!pending.IsCompleted)
            {
                var status = $"{frames[frame++ % frames.Length]} {label} ({clock.Elapsed.TotalSeconds:F1}s)";
                width = Math.Max(width, status.Length);
                // A failed progress display must not abandon a running model request.
                if (!TryWrite(output, "\r" + status.PadRight(width))) break;
                await Task.WhenAny(pending, Task.Delay(120));
            }
            return await pending;
        }
        finally
        {
            if (width > 0) TryWrite(output, "\r" + new string(' ', width) + "\r");
        }
    }

    private static bool TryWrite(TextWriter output, string text)
    {
        try { output.Write(text); output.Flush(); return true; }
        catch (IOException) { return false; }
        catch (ObjectDisposedException) { return false; }
    }
}

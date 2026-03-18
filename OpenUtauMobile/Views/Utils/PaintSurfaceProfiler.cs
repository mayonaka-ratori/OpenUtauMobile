#if DEBUG
using System.Diagnostics;

namespace OpenUtauMobile.Views.Utils;

/// <summary>
/// Lightweight profiler for PaintSurface handlers. DEBUG builds only.
/// Logs frame times and tracks slow frames (> 8ms target).
/// </summary>
public static class PaintSurfaceProfiler
{
    private const double TargetMs = 8.0;
    private static readonly Dictionary<string, (long totalFrames, long slowFrames, double maxMs)> _stats = new();

    public static Stopwatch Start() => Stopwatch.StartNew();

    public static void End(Stopwatch sw, string canvasName)
    {
        sw.Stop();
        double ms = sw.Elapsed.TotalMilliseconds;

        if (!_stats.TryGetValue(canvasName, out var stat))
        {
            stat = (0, 0, 0);
        }

        stat.totalFrames++;
        if (ms > stat.maxMs) stat.maxMs = ms;
        if (ms > TargetMs) stat.slowFrames++;
        _stats[canvasName] = stat;

        // Log slow frames immediately
        if (ms > TargetMs)
        {
            Debug.WriteLine($"⚠️ SLOW FRAME [{canvasName}]: {ms:F2}ms (target: {TargetMs}ms)");
        }
    }

    /// <summary>
    /// Dump accumulated stats. Call from a debug button or on page dispose.
    /// </summary>
    public static void DumpStats()
    {
        Debug.WriteLine("=== PaintSurface Performance Stats ===");
        foreach (var (name, stat) in _stats.OrderByDescending(x => x.Value.maxMs))
        {
            double slowPercent = stat.totalFrames > 0 ? (double)stat.slowFrames / stat.totalFrames * 100 : 0;
            Debug.WriteLine($"  {name}: {stat.totalFrames} frames, max={stat.maxMs:F2}ms, slow(>{TargetMs}ms)={stat.slowFrames} ({slowPercent:F1}%)");
        }
        Debug.WriteLine("======================================");
    }

    public static void Reset()
    {
        _stats.Clear();
    }
}
#endif

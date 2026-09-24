using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenScanSim.Platform.Windows.Native;

namespace OpenScanSim.Platform.Windows;

/// <summary>
/// High-resolution multimedia timer service enforcing 1.0ms Windows timer resolution.
/// </summary>
public sealed class HighResolutionTimer : IDisposable
{
    private static readonly double StopwatchFrequency = Stopwatch.Frequency;
    private bool _disposed;

    public HighResolutionTimer()
    {
        if (OperatingSystem.IsWindows())
        {
            WinMm.TimeBeginPeriod(1);
        }
    }

    /// <summary>
    /// Delays execution with sub-millisecond precision using a hybrid async sleep and high-resolution spin wait.
    /// </summary>
    public static async Task DelayPreciseAsync(double milliseconds, CancellationToken cancellationToken = default)
    {
        if (milliseconds <= 0) return;

        long start = Stopwatch.GetTimestamp();
        long targetTicks = (long)(milliseconds * StopwatchFrequency / 1000.0);

        // For large delays (>15ms), sleep asynchronously for the majority of the time to save CPU
        if (milliseconds > 16)
        {
            int coarseDelay = (int)(milliseconds - 12);
            await Task.Delay(coarseDelay, cancellationToken).ConfigureAwait(false);
        }

        // Spin-wait for the remaining sub-millisecond fraction
        while (Stopwatch.GetTimestamp() - start < targetTicks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Thread.SpinWait(10);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (OperatingSystem.IsWindows())
            {
                WinMm.TimeEndPeriod(1);
            }
            _disposed = true;
        }
    }
}

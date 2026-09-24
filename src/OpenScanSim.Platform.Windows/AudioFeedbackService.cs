using System;
using System.Threading.Tasks;
using OpenScanSim.Core.Services;

namespace OpenScanSim.Platform.Windows;

/// <summary>
/// Hardware scanner audio synthesizer providing instant beep feedback on simulated reads.
/// </summary>
public sealed class AudioFeedbackService : IAudioFeedbackService
{
    public void PlayScanBeep(int pitchHz = 2400, int durationMs = 60)
    {
        Task.Run(() =>
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Console.Beep(Math.Clamp(pitchHz, 200, 8000), Math.Clamp(durationMs, 10, 500));
                }
            }
            catch
            {
                // Silent fallback if audio device is unavailable or muted
            }
        });
    }

    public void PlayErrorBeep(int pitchHz = 800, int durationMs = 200)
    {
        Task.Run(() =>
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Console.Beep(Math.Clamp(pitchHz, 200, 8000), Math.Clamp(durationMs, 50, 1000));
                }
            }
            catch
            {
                // Silent fallback
            }
        });
    }
}

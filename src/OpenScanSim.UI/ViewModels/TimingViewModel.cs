using CommunityToolkit.Mvvm.ComponentModel;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class TimingViewModel : ViewModelBase
{
    [ObservableProperty]
    private SimulationMode _mode = SimulationMode.FastDeviceBurst;

    [ObservableProperty]
    private int _humanTypingSpeedMs = 50;

    [ObservableProperty]
    private int _humanJitterMs = 25;

    [ObservableProperty]
    private double _typoChancePercent = 1.0;

    [ObservableProperty]
    private double _startCountdownSeconds = 3.0;

    [ObservableProperty]
    private double _intervalSeconds = 0.1;

    [ObservableProperty]
    private int _burstBatchSize = 1;

    [ObservableProperty]
    private bool _loopContinuously = false;

    [ObservableProperty]
    private string _prefix = string.Empty;

    [ObservableProperty]
    private OutputTerminator _terminator = OutputTerminator.Enter;

    [ObservableProperty]
    private string _customTerminator = string.Empty;

    [ObservableProperty]
    private bool _audioFeedbackEnabled = true;

    [ObservableProperty]
    private int _audioPitchHz = 2400;

    [ObservableProperty]
    private int _audioDurationMs = 60;

    public TimingConfig BuildTimingConfig()
    {
        return new TimingConfig
        {
            StartCountdownSeconds = StartCountdownSeconds,
            IntervalSeconds = IntervalSeconds,
            BurstBatchSize = BurstBatchSize,
            LoopContinuously = LoopContinuously
        };
    }

    public WedgeConfig BuildWedgeConfig()
    {
        return new WedgeConfig
        {
            Mode = Mode,
            HumanTypingSpeedMs = HumanTypingSpeedMs,
            HumanJitterMs = HumanJitterMs,
            TypoChancePercent = TypoChancePercent,
            Prefix = Prefix,
            Terminator = Terminator,
            CustomTerminator = CustomTerminator,
            AudioFeedbackEnabled = AudioFeedbackEnabled,
            AudioPitchHz = AudioPitchHz,
            AudioDurationMs = AudioDurationMs
        };
    }
}

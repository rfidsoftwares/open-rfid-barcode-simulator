using System;
using System.Threading;
using System.Threading.Tasks;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;

namespace OpenScanSim.Core.Services;

/// <summary>
/// Audio feedback service for hardware scanner beep simulation.
/// </summary>
public interface IAudioFeedbackService
{
    void PlayScanBeep(int pitchHz = 2400, int durationMs = 60);
    void PlayErrorBeep(int pitchHz = 800, int durationMs = 200);
}

/// <summary>
/// Progress telemetry payload during simulation execution.
/// </summary>
public readonly record struct SimulationProgress(
    SimulationState State,
    ulong ProcessedCount,
    ulong TotalCount,
    double CountdownRemainingSeconds,
    string? CurrentPayload,
    double CurrentTagsPerSecond);

/// <summary>
/// Master orchestrator interface for executing streaming simulation runs.
/// </summary>
public interface ISimulationOrchestrator
{
    SimulationState CurrentState { get; }
    event Action<SimulationProgress>? ProgressChanged;
    event Action<ScanRecord>? ScanTransmitted;
    event Action<string>? ErrorOccurred;

    Task StartAsync(GeneratorConfig generatorConfig, TimingConfig timingConfig, WedgeConfig wedgeConfig, CancellationToken cancellationToken = default);
    void Pause();
    void Resume();
    void Stop();
    Task TriggerSingleStepAsync(GeneratorConfig generatorConfig, WedgeConfig wedgeConfig, CancellationToken cancellationToken = default);
}

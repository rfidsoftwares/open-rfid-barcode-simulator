using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Generators;

namespace OpenScanSim.Core.Services;

/// <summary>
/// Master streaming orchestrator coordinating data generation, keystroke simulation, timers, and progress telemetry.
/// </summary>
public sealed class SimulationOrchestrator : ISimulationOrchestrator
{
    private readonly IScanDataGenerator _generator;
    private readonly IKeyboardSimulator _keyboard;
    private readonly IAudioFeedbackService _audio;

    private readonly SemaphoreSlim _pauseLock = new(1, 1);
    private CancellationTokenSource? _activeCts;
    private volatile SimulationState _state = SimulationState.Idle;

    public SimulationState CurrentState => _state;
    public event Action<SimulationProgress>? ProgressChanged;
    public event Action<ScanRecord>? ScanTransmitted;
    public event Action<string>? ErrorOccurred;

    public SimulationOrchestrator(
        IScanDataGenerator generator,
        IKeyboardSimulator keyboard,
        IAudioFeedbackService audio)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
    }

    public async Task StartAsync(
        GeneratorConfig generatorConfig,
        TimingConfig timingConfig,
        WedgeConfig wedgeConfig,
        CancellationToken cancellationToken = default)
    {
        Stop(); // Cancel any existing run

        _activeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _activeCts.Token;

        try
        {
            // 1. Initial Countdown Phase
            if (timingConfig.StartCountdownSeconds > 0)
            {
                SetState(SimulationState.StartingCountdown);
                double remaining = timingConfig.StartCountdownSeconds;

                while (remaining > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    NotifyProgress(0, generatorConfig.TotalCount, remaining, null, 0.0);

                    double step = Math.Min(0.1, remaining);
                    await Task.Delay(TimeSpan.FromSeconds(step), ct).ConfigureAwait(false);
                    remaining -= step;
                }
            }

            // 2. Active Streaming Transmission Phase
            SetState(SimulationState.Running);
            ulong processedCount = 0;
            ulong totalCount = generatorConfig.TotalCount;
            int burstCounter = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            do
            {
                await foreach (var record in _generator.GenerateStreamAsync(generatorConfig, ct).ConfigureAwait(false))
                {
                    ct.ThrowIfCancellationRequested();

                    // Handle Pause State
                    await _pauseLock.WaitAsync(ct).ConfigureAwait(false);
                    _pauseLock.Release();

                    // Format payload
                    string outputString = record.ToWedgeOutput(
                        includeTid: generatorConfig.IncludeTid,
                        includeUser: generatorConfig.IncludeUserMemory);

                    // Send keystrokes
                    await _keyboard.SendKeystrokesAsync(outputString, wedgeConfig, ct).ConfigureAwait(false);

                    // Audio feedback
                    if (wedgeConfig.AudioFeedbackEnabled)
                    {
                        _audio.PlayScanBeep(wedgeConfig.AudioPitchHz, wedgeConfig.AudioDurationMs);
                    }

                    processedCount++;
                    burstCounter++;

                    // Calculate TPS throughput
                    double elapsedSec = stopwatch.Elapsed.TotalSeconds;
                    double tps = elapsedSec > 0.01 ? processedCount / elapsedSec : 0.0;

                    ScanTransmitted?.Invoke(record);
                    NotifyProgress(processedCount, totalCount, 0.0, outputString, tps);

                    // Check burst threshold & interval wait
                    if (burstCounter >= timingConfig.BurstBatchSize)
                    {
                        burstCounter = 0;
                        if (timingConfig.IntervalSeconds > 0)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(timingConfig.IntervalSeconds), ct).ConfigureAwait(false);
                        }
                    }
                }
            }
            while (timingConfig.LoopContinuously && !ct.IsCancellationRequested);

            SetState(SimulationState.Completed);
            NotifyProgress(processedCount, totalCount, 0.0, null, 0.0);
        }
        catch (OperationCanceledException)
        {
            SetState(SimulationState.Idle);
            NotifyProgress(0, generatorConfig.TotalCount, 0.0, null, 0.0);
        }
        catch (Exception ex)
        {
            SetState(SimulationState.Faulted);
            ErrorOccurred?.Invoke(ex.Message);
            _audio.PlayErrorBeep();
        }
    }

    public void Pause()
    {
        if (_state == SimulationState.Running)
        {
            _pauseLock.Wait(0);
            SetState(SimulationState.Paused);
        }
    }

    public void Resume()
    {
        if (_state == SimulationState.Paused)
        {
            if (_pauseLock.CurrentCount == 0)
            {
                _pauseLock.Release();
            }
            SetState(SimulationState.Running);
        }
    }

    public void Stop()
    {
        if (_state != SimulationState.Idle)
        {
            _activeCts?.Cancel();
            _activeCts?.Dispose();
            _activeCts = null;

            if (_pauseLock.CurrentCount == 0)
            {
                _pauseLock.Release();
            }

            SetState(SimulationState.Idle);
        }
    }

    public async Task TriggerSingleStepAsync(
        GeneratorConfig generatorConfig,
        WedgeConfig wedgeConfig,
        CancellationToken cancellationToken = default)
    {
        var singleConfig = new GeneratorConfig
        {
            ReaderType = generatorConfig.ReaderType,
            MaskPattern = generatorConfig.MaskPattern,
            StartSequence = generatorConfig.StartSequence,
            TotalCount = 1,
            Symbology = generatorConfig.Symbology,
            IncludeCheckDigit = generatorConfig.IncludeCheckDigit,
            IncludeAimPrefix = generatorConfig.IncludeAimPrefix,
            IncludeTid = generatorConfig.IncludeTid,
            IncludeUserMemory = generatorConfig.IncludeUserMemory
        };

        // F10 / Step is always an instant burst scan (<0.1ms) with zero countdown or inter-char delay
        var instantWedgeConfig = new WedgeConfig
        {
            Mode = SimulationMode.FastDeviceBurst,
            Prefix = wedgeConfig.Prefix,
            Terminator = wedgeConfig.Terminator,
            CustomTerminator = wedgeConfig.CustomTerminator,
            AudioFeedbackEnabled = wedgeConfig.AudioFeedbackEnabled,
            AudioPitchHz = wedgeConfig.AudioPitchHz,
            AudioDurationMs = wedgeConfig.AudioDurationMs
        };

        await foreach (var record in _generator.GenerateStreamAsync(singleConfig, cancellationToken).ConfigureAwait(false))
        {
            string output = record.ToWedgeOutput(includeTid: singleConfig.IncludeTid, includeUser: singleConfig.IncludeUserMemory);
            await _keyboard.SendKeystrokesAsync(output, instantWedgeConfig, cancellationToken).ConfigureAwait(false);

            if (instantWedgeConfig.AudioFeedbackEnabled)
            {
                _audio.PlayScanBeep(instantWedgeConfig.AudioPitchHz, instantWedgeConfig.AudioDurationMs);
            }

            ScanTransmitted?.Invoke(record);
            NotifyProgress(1, 1, 0.0, output, 0.0);
            break;
        }
    }

    private void SetState(SimulationState newState)
    {
        _state = newState;
    }

    private void NotifyProgress(ulong processed, ulong total, double countdown, string? payload, double tps)
    {
        ProgressChanged?.Invoke(new SimulationProgress(
            State: _state,
            ProcessedCount: processed,
            TotalCount: total,
            CountdownRemainingSeconds: countdown,
            CurrentPayload: payload,
            CurrentTagsPerSecond: tps));
    }
}

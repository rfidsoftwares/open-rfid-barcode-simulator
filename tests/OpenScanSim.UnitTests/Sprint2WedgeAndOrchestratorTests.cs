using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Generators;
using OpenScanSim.Core.Services;
using OpenScanSim.Platform.Windows;
using Xunit;

namespace OpenScanSim.UnitTests;

public class MockKeyboardSimulator : IKeyboardSimulator
{
    public List<string> InjectedKeystrokes { get; } = [];

    public Task SendKeystrokesAsync(string text, WedgeConfig config, CancellationToken cancellationToken = default)
    {
        InjectedKeystrokes.Add(text);
        return Task.CompletedTask;
    }

    public Task SendKeyAsync(ushort virtualKey, bool extendedKey = false, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class MockAudioFeedbackService : IAudioFeedbackService
{
    public int BeepCount { get; private set; }
    public int ErrorBeepCount { get; private set; }

    public void PlayScanBeep(int pitchHz = 2400, int durationMs = 60) => BeepCount++;
    public void PlayErrorBeep(int pitchHz = 800, int durationMs = 200) => ErrorBeepCount++;
}

public class Sprint2WedgeAndOrchestratorTests
{
    [Fact]
    public async Task Orchestrator_ExecutesFullSimulationLifecycle()
    {
        // Arrange
        MaskPatternGenerator generator = new();
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();

        SimulationOrchestrator orchestrator = new(generator, keyboard, audio);

        GeneratorConfig genConfig = new()
        {
            ReaderType = ReaderType.UhfRfid,
            MaskPattern = "3034{HEX:4}{SEQ:4}",
            TotalCount = 5,
            StartSequence = 100,
            IncludeTid = false
        };

        TimingConfig timingConfig = new()
        {
            StartCountdownSeconds = 0.0, // Instant start for test
            IntervalSeconds = 0.001,
            BurstBatchSize = 1
        };

        WedgeConfig wedgeConfig = new()
        {
            Mode = SimulationMode.FastDeviceBurst,
            AudioFeedbackEnabled = true
        };

        List<SimulationProgress> progressHistory = [];
        orchestrator.ProgressChanged += progressHistory.Add;

        // Act
        await orchestrator.StartAsync(genConfig, timingConfig, wedgeConfig);

        // Assert
        keyboard.InjectedKeystrokes.Should().HaveCount(5);
        keyboard.InjectedKeystrokes[0].Should().StartWith("3034").And.EndWith("0100");
        audio.BeepCount.Should().Be(5);
        orchestrator.CurrentState.Should().Be(SimulationState.Completed);
    }

    [Fact]
    public async Task Orchestrator_SingleStep_SendsExactlyOneItem()
    {
        // Arrange
        MaskPatternGenerator generator = new();
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();

        SimulationOrchestrator orchestrator = new(generator, keyboard, audio);

        GeneratorConfig genConfig = new()
        {
            ReaderType = ReaderType.UhfRfid,
            MaskPattern = "30340000{SEQ:4}",
            StartSequence = 42,
            IncludeTid = false
        };

        WedgeConfig wedgeConfig = new()
        {
            AudioFeedbackEnabled = true
        };

        // Act
        await orchestrator.TriggerSingleStepAsync(genConfig, wedgeConfig);

        // Assert
        keyboard.InjectedKeystrokes.Should().ContainSingle();
        keyboard.InjectedKeystrokes[0].Should().Be("303400000042");
        audio.BeepCount.Should().Be(1);
    }

    [Fact]
    public void HighResolutionTimer_InitializesAndDisposesGracefully()
    {
        // Act & Assert: Must not throw on initialization or disposal
        using HighResolutionTimer timer = new();
        timer.Should().NotBeNull();
    }
}

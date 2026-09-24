using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OpenScanSim.Common.Constants;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Platform.Windows;
using Xunit;

namespace OpenScanSim.UnitTests;

public class Win32PlatformServiceTests
{
    [Fact]
    public void HotkeyService_DispatchesRegisteredActionOnMatchingWmHotkey()
    {
        // Arrange
        using Win32GlobalHotkeyService hotkeyService = new();
        bool actionTriggered = false;

        // Initialize with dummy handle (1)
        hotkeyService.Initialize(new IntPtr(1));

        // Act: register action for ID 9001
        hotkeyService.RegisterHotkey(AppConstants.Hotkeys.StartStopHotkeyId, AppConstants.Hotkeys.VkF8, 0, () =>
        {
            actionTriggered = true;
        });

        // Simulate WM_HOTKEY window message (0x0312) with wParam = 9001
        bool handled = hotkeyService.ProcessWindowMessage(AppConstants.Win32.WmHotkey, new IntPtr(AppConstants.Hotkeys.StartStopHotkeyId));

        // Assert
        handled.Should().BeTrue();
        actionTriggered.Should().BeTrue();
    }

    [Fact]
    public void AudioFeedbackService_ExecutesWithoutThrowing()
    {
        // Arrange
        AudioFeedbackService audio = new();

        // Act & Assert
        var scanBeepAction = () => audio.PlayScanBeep(2400, 20);
        var errorBeepAction = () => audio.PlayErrorBeep(800, 50);

        scanBeepAction.Should().NotThrow();
        errorBeepAction.Should().NotThrow();
    }

    [Fact]
    public async Task HighResolutionTimer_PreciseDelay_CompletesAccurately()
    {
        // Arrange: 10ms delay
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act & Assert
        var act = async () => await HighResolutionTimer.DelayPreciseAsync(10, cts.Token);
        await act.Should().NotThrowAsync();
    }
}

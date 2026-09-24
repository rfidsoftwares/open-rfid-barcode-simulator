using System;
using System.Threading.Tasks;
using FluentAssertions;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Generators;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Services;
using OpenScanSim.Core.Validators;
using OpenScanSim.UI.ViewModels;
using Xunit;

namespace OpenScanSim.UnitTests;

public class ViewModelTests
{
    [Fact]
    public void GeneratorViewModel_PreviewUpdatesOnPatternChange()
    {
        // Arrange
        GeneratorViewModel vm = new();

        // Act
        vm.MaskPattern = "3034{HEX:4}{SEQ:4}";
        vm.StartSequence = 99;

        // Assert
        vm.PreviewPayload.Should().HaveLength(12);
        vm.PreviewPayload.Should().StartWith("3034");
        vm.PreviewPayload.Should().EndWith("0099");
    }

    [Fact]
    public void GeneratorViewModel_PastedList_UpdatesCountAndPreview()
    {
        // Arrange
        GeneratorViewModel vm = new();

        // Act - Load sample 20 EPC list
        vm.LoadSamplePastedListCommand.Execute(null);

        // Assert
        vm.IsPastedMode.Should().BeTrue();
        vm.PastedItemCount.Should().Be(20);
        vm.TotalCount.Should().Be(20);
        vm.PreviewPayload.Should().StartWith("E28033000F000F0F3BC9B222");

        var config = vm.BuildGeneratorConfig(ReaderType.UhfRfid);
        config.DirectPastedList.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DashboardViewModel_ProtocolSwitchingUpdatesSelectedTypeAndMasks()
    {
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();
        MaskPatternGenerator generator = new();
        SimulationOrchestrator orchestrator = new(generator, keyboard, audio);
        GeneratorViewModel genVm = new();
        TimingViewModel timingVm = new();

        DashboardViewModel dashVm = new(orchestrator, genVm, timingVm);

        dashVm.SelectedReaderType.Should().Be(ReaderType.UhfRfid);

        // Switch to Barcode
        dashVm.IsBarcodeSelected = true;
        dashVm.SelectedReaderType.Should().Be(ReaderType.Barcode1D2D);
        dashVm.Generator.MaskPattern.Should().Be("400638133393");

        // Switch to HF RFID
        dashVm.IsHfSelected = true;
        dashVm.SelectedReaderType.Should().Be(ReaderType.HfRfid);

        // Switch to NFC
        dashVm.IsNfcSelected = true;
        dashVm.SelectedReaderType.Should().Be(ReaderType.Nfc);

        // Switch back to UHF
        dashVm.IsUhfSelected = true;
        dashVm.SelectedReaderType.Should().Be(ReaderType.UhfRfid);
    }

    [Fact]
    public async Task DashboardViewModel_WhenBarcodeSelected_TransmitsBarcodeRecordInLogs()
    {
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();
        MaskPatternGenerator generator = new();
        SimulationOrchestrator orchestrator = new(generator, keyboard, audio);
        GeneratorViewModel genVm = new();
        TimingViewModel timingVm = new();

        DashboardViewModel dashVm = new(orchestrator, genVm, timingVm);

        // Select Barcode
        dashVm.IsBarcodeSelected = true;
        dashVm.SelectedReaderType.Should().Be(ReaderType.Barcode1D2D);

        // Trigger step
        await dashVm.TriggerSingleStepAsync();

        // Verify logs
        dashVm.RecentTransmissions.Should().NotBeEmpty();
        dashVm.RecentTransmissions[0].Should().Contain("Barcode1D2D");
    }

    [Fact]
    public void DashboardViewModel_TotalCount_UpdatesImmediatelyWhenGeneratorTotalCountChanges()
    {
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();
        MaskPatternGenerator generator = new();
        SimulationOrchestrator orchestrator = new(generator, keyboard, audio);
        GeneratorViewModel genVm = new();
        TimingViewModel timingVm = new();

        DashboardViewModel dashVm = new(orchestrator, genVm, timingVm);
        dashVm.TotalCount.Should().Be(100);

        // Modify Generator TotalCount directly without starting simulation
        genVm.TotalCount = 5000;

        // Verify DashboardViewModel immediately reflects 5000
        dashVm.TotalCount.Should().Be(5000);
    }

    [Fact]
    public void InspectorViewModel_RecalculatesImmediatelyWhenPropertiesChange()
    {
        InspectorViewModel vm = new();
        string initialEpc = vm.CalculatedEpcHex;

        vm.ItemReference = 999;
        vm.CalculatedEpcHex.Should().NotBe(initialEpc);
    }

    [Fact]
    public void InspectorViewModel_CalculatesSgtin96Correctly()
    {
        // Arrange
        InspectorViewModel vm = new()
        {
            FilterValue = 3,
            Partition = 2,
            CompanyPrefix = 614141,
            ItemReference = 100,
            SerialNumber = 1
        };

        // Act
        vm.CalculateTag();

        // Assert
        vm.CalculatedEpcHex.Should().HaveLength(24);
        vm.CalculatedEpcHex.Should().StartWith("30");
        vm.CalculatedPcWord.Should().Be("3000");
        vm.CalculatedCrc16.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MainViewModel_NavigationSwitchesPages()
    {
        // Arrange
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();
        MaskPatternGenerator generator = new();
        SimulationOrchestrator orchestrator = new(generator, keyboard, audio);
        ProtocolValidator validator = new();

        GeneratorViewModel genVm = new();
        TimingViewModel timingVm = new();
        DashboardViewModel dashVm = new(orchestrator, genVm, timingVm);
        InspectorViewModel inspVm = new();
        BarcodeStudioViewModel barcodeVm = new(keyboard, audio, validator, timingVm);
        NdefStudioViewModel ndefVm = new(keyboard, audio, timingVm);
        ExtendedStudioViewModel extVm = new(keyboard, audio, timingVm);
        TelemetryViewModel teleVm = new(orchestrator);

        MainViewModel mainVm = new(dashVm, genVm, timingVm, inspVm, barcodeVm, ndefVm, extVm, teleVm);

        // Act & Assert
        mainVm.Navigate(0);
        mainVm.CurrentPage.Should().Be(dashVm);

        mainVm.Navigate(1);
        mainVm.CurrentPage.Should().Be(genVm);

        mainVm.Navigate(2);
        mainVm.CurrentPage.Should().Be(timingVm);

        mainVm.Navigate(3);
        mainVm.CurrentPage.Should().Be(inspVm);

        mainVm.Navigate(4);
        mainVm.CurrentPage.Should().Be(barcodeVm);

        mainVm.Navigate(5);
        mainVm.CurrentPage.Should().Be(ndefVm);

        mainVm.Navigate(6);
        mainVm.CurrentPage.Should().Be(extVm);

        mainVm.Navigate(7);
        mainVm.CurrentPage.Should().Be(teleVm);
    }

    [Fact]
    public void BarcodeStudioViewModel_FormatEan13WithCheckDigit()
    {
        // Arrange
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();
        ProtocolValidator validator = new();
        TimingViewModel timingVm = new();
        BarcodeStudioViewModel vm = new(keyboard, audio, validator, timingVm);

        // Act
        vm.Symbology = BarcodeSymbology.Ean13;
        vm.RawPayload = "400638133393";
        vm.IncludeCheckDigit = true;

        // Assert
        vm.FormattedOutput.Should().Be("4006381333931");
        vm.ValidationStatus.Should().Contain("✓");
    }

    [Fact]
    public void NdefStudioViewModel_GeneratesValidUriRecord()
    {
        // Arrange
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();
        TimingViewModel timingVm = new();
        NdefStudioViewModel vm = new(keyboard, audio, timingVm);

        // Act
        vm.RecordTypeIndex = 0;
        vm.UriInput = "https://rfidsoftwares.com/";

        // Assert
        vm.HexOutput.Should().StartWith("D101");
        vm.ByteLength.Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task TelemetryViewModel_AuditTrailAndCsvExport()
    {
        // Arrange
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();
        MaskPatternGenerator generator = new();
        SimulationOrchestrator orchestrator = new(generator, keyboard, audio);
        TelemetryViewModel vm = new(orchestrator);

        // Act: Trigger single step simulation
        GeneratorConfig genConfig = new()
        {
            ReaderType = ReaderType.UhfRfid,
            MaskPattern = "30340000{SEQ:4}",
            StartSequence = 1,
            TotalCount = 1
        };
        WedgeConfig wedgeConfig = new();
        await orchestrator.TriggerSingleStepAsync(genConfig, wedgeConfig);

        // Assert
        vm.TransmissionAuditTrail.Should().ContainSingle();
        vm.ExportAuditTrailCsvCommand.Execute(null);
        vm.LastExportedCsv.Should().Contain("TimestampUtc,Sequence,ReaderType");
        vm.LastExportedCsv.Should().Contain("303400000001");
    }

    [Fact]
    public async Task DashboardViewModel_StartSimulation_UpdatesTelemetry()
    {
        // Arrange
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();
        MaskPatternGenerator generator = new();
        SimulationOrchestrator orchestrator = new(generator, keyboard, audio);

        GeneratorViewModel genVm = new()
        {
            TotalCount = 3,
            MaskPattern = "30340000{SEQ:4}"
        };

        TimingViewModel timingVm = new()
        {
            StartCountdownSeconds = 0.0,
            IntervalSeconds = 0.001
        };

        DashboardViewModel dashVm = new(orchestrator, genVm, timingVm);

        // Act
        await dashVm.StartSimulationAsync();

        // Assert
        dashVm.ProcessedCount.Should().Be(3);
        dashVm.StatusBadgeText.Should().Be("COMPLETED");
        dashVm.RecentTransmissions.Should().HaveCount(3);
    }

    [Fact]
    public void FloatingHudViewModel_ForwardsEventsAndSharesDashboardState()
    {
        // Arrange
        MockKeyboardSimulator keyboard = new();
        MockAudioFeedbackService audio = new();
        MaskPatternGenerator generator = new();
        SimulationOrchestrator orchestrator = new(generator, keyboard, audio);

        GeneratorViewModel genVm = new();
        TimingViewModel timingVm = new();
        DashboardViewModel dashVm = new(orchestrator, genVm, timingVm);
        FloatingHudViewModel hudVm = new(dashVm);

        bool showMainRequested = false;
        bool closeHudRequested = false;

        hudVm.RequestShowMainWindow += () => showMainRequested = true;
        hudVm.RequestCloseHud += () => closeHudRequested = true;

        // Act
        hudVm.OpenMainWindowCommand.Execute(null);
        hudVm.CloseHudCommand.Execute(null);

        // Assert
        showMainRequested.Should().BeTrue();
        closeHudRequested.Should().BeTrue();
        hudVm.Dashboard.Should().BeSameAs(dashVm);
    }
}

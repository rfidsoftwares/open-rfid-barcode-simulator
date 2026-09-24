using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Services;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class TelemetryViewModel : ViewModelBase
{
    private readonly ISimulationOrchestrator _orchestrator;
    private double _peakTps;

    [ObservableProperty]
    private double _currentTps;

    [ObservableProperty]
    private double _peakTpsDisplay;

    [ObservableProperty]
    private ulong _totalTransmitted;

    [ObservableProperty]
    private string _sessionStateText = "IDLE";

    public ObservableCollection<ScanRecord> TransmissionAuditTrail { get; } = [];

    public TelemetryViewModel(ISimulationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

        _orchestrator.ProgressChanged += OnProgressChanged;
        _orchestrator.ScanTransmitted += OnScanTransmitted;
    }

    private void OnProgressChanged(SimulationProgress progress)
    {
        CurrentTps = progress.CurrentTagsPerSecond;
        TotalTransmitted = progress.ProcessedCount;
        SessionStateText = progress.State.ToString().ToUpperInvariant();

        if (progress.CurrentTagsPerSecond > _peakTps)
        {
            _peakTps = progress.CurrentTagsPerSecond;
            PeakTpsDisplay = _peakTps;
        }
    }

    private void OnScanTransmitted(ScanRecord record)
    {
        void UpdateAuditList()
        {
            if (TransmissionAuditTrail.Count > 200)
            {
                TransmissionAuditTrail.RemoveAt(TransmissionAuditTrail.Count - 1);
            }
            TransmissionAuditTrail.Insert(0, record);
        }

        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            UpdateAuditList();
        }
        else
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(UpdateAuditList);
        }
    }

    [ObservableProperty]
    private string _lastExportedCsv = string.Empty;

    [RelayCommand]
    public void ClearAuditTrail()
    {
        TransmissionAuditTrail.Clear();
        TotalTransmitted = 0;
        CurrentTps = 0;
        _peakTps = 0;
        PeakTpsDisplay = 0;
        LastExportedCsv = string.Empty;
    }

    [RelayCommand]
    public void ExportAuditTrailCsv()
    {
        LastExportedCsv = GenerateCsvContent();
    }

    public string GenerateCsvContent()
    {
        StringBuilder sb = new();
        sb.AppendLine("TimestampUtc,Sequence,ReaderType,PrimaryPayload,PC,TID,UserMemory");

        foreach (var r in TransmissionAuditTrail)
        {
            sb.AppendLine($"{r.Timestamp:o},{r.SequenceIndex},{r.ReaderType},{r.PrimaryPayload},{r.ProtocolControlWord},{r.Tid},{r.UserMemoryHex}");
        }

        return sb.ToString();
    }
}

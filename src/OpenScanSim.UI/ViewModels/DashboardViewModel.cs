using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenScanSim.Common.Constants;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Services;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly ISimulationOrchestrator _orchestrator;
    private readonly GeneratorViewModel _generatorVm;
    private readonly TimingViewModel _timingVm;

    public GeneratorViewModel Generator => _generatorVm;
    public TimingViewModel Timing => _timingVm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUhfSelected))]
    [NotifyPropertyChangedFor(nameof(IsBarcodeSelected))]
    [NotifyPropertyChangedFor(nameof(IsHfSelected))]
    [NotifyPropertyChangedFor(nameof(IsNfcSelected))]
    private ReaderType _selectedReaderType = ReaderType.UhfRfid;

    public bool IsUhfSelected
    {
        get => SelectedReaderType == ReaderType.UhfRfid;
        set { if (value) SelectedReaderType = ReaderType.UhfRfid; }
    }

    public bool IsBarcodeSelected
    {
        get => SelectedReaderType == ReaderType.Barcode1D2D;
        set { if (value) SelectedReaderType = ReaderType.Barcode1D2D; }
    }

    public bool IsHfSelected
    {
        get => SelectedReaderType == ReaderType.HfRfid;
        set { if (value) SelectedReaderType = ReaderType.HfRfid; }
    }

    public bool IsNfcSelected
    {
        get => SelectedReaderType == ReaderType.Nfc;
        set { if (value) SelectedReaderType = ReaderType.Nfc; }
    }

    [ObservableProperty]
    private SimulationState _currentState = SimulationState.Idle;

    [ObservableProperty]
    private ulong _processedCount;

    public ulong TotalCount
    {
        get => _generatorVm.TotalCount;
        set
        {
            if (_generatorVm.TotalCount != value)
            {
                _generatorVm.TotalCount = value;
                OnPropertyChanged();
                UpdateProgressPercent();
            }
        }
    }

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private double _countdownRemaining;

    [ObservableProperty]
    private double _currentTps;

    [ObservableProperty]
    private string _lastPayload = "Ready to simulate...";

    [ObservableProperty]
    private string _statusBadgeText = "IDLE";

    [ObservableProperty]
    private string _statusBadgeColor = "#64748B";

    public ObservableCollection<string> RecentTransmissions { get; } = [];

    public DashboardViewModel(
        ISimulationOrchestrator orchestrator,
        GeneratorViewModel generatorVm,
        TimingViewModel timingVm)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _generatorVm = generatorVm ?? throw new ArgumentNullException(nameof(generatorVm));
        _timingVm = timingVm ?? throw new ArgumentNullException(nameof(timingVm));

        _orchestrator.ProgressChanged += OnProgressChanged;
        _orchestrator.ScanTransmitted += OnScanTransmitted;
        _orchestrator.ErrorOccurred += OnErrorOccurred;

        _generatorVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(GeneratorViewModel.TotalCount))
            {
                OnPropertyChanged(nameof(TotalCount));
                UpdateProgressPercent();
            }
            else if (e.PropertyName == nameof(GeneratorViewModel.MaskPattern))
            {
                OnPropertyChanged(nameof(Generator));
            }
        };

        _timingVm.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Timing));
        };
    }

    private void UpdateProgressPercent()
    {
        if (TotalCount > 0)
        {
            ProgressPercent = Math.Clamp((double)ProcessedCount / TotalCount * 100.0, 0, 100);
        }
    }

    partial void OnSelectedReaderTypeChanged(ReaderType value)
    {
        switch (value)
        {
            case ReaderType.UhfRfid:
                Generator.MaskPattern = AppConstants.Defaults.DefaultUhfMask;
                break;
            case ReaderType.Barcode1D2D:
                Generator.MaskPattern = "400638133393";
                break;
            case ReaderType.HfRfid:
                Generator.MaskPattern = "04{HEX:12}";
                break;
            case ReaderType.Nfc:
                Generator.MaskPattern = "https://rfidsoftwares.com/tag/{SEQ:6}";
                break;
        }
    }

    [RelayCommand]
    public async Task StartSimulationAsync()
    {
        if (CurrentState == SimulationState.Running || CurrentState == SimulationState.StartingCountdown)
            return;

        var genConfig = _generatorVm.BuildGeneratorConfig(SelectedReaderType);
        var timingConfig = _timingVm.BuildTimingConfig();
        var wedgeConfig = _timingVm.BuildWedgeConfig();

        ProcessedCount = 0;
        ProgressPercent = 0;
        CurrentTps = 0;

        await _orchestrator.StartAsync(genConfig, timingConfig, wedgeConfig);
    }

    [RelayCommand]
    public void StopSimulation()
    {
        _orchestrator.Stop();
        CurrentState = SimulationState.Idle;
        UpdateStatusBadge();
    }

    [RelayCommand]
    public void PauseSimulation()
    {
        _orchestrator.Pause();
        CurrentState = SimulationState.Paused;
        UpdateStatusBadge();
    }

    [RelayCommand]
    public void ResumeSimulation()
    {
        _orchestrator.Resume();
        CurrentState = SimulationState.Running;
        UpdateStatusBadge();
    }

    [RelayCommand]
    public async Task TriggerSingleStepAsync()
    {
        var genConfig = _generatorVm.BuildGeneratorConfig(SelectedReaderType);
        var wedgeConfig = _timingVm.BuildWedgeConfig();
        await _orchestrator.TriggerSingleStepAsync(genConfig, wedgeConfig);
    }

    private void OnProgressChanged(SimulationProgress progress)
    {
        void UpdateState()
        {
            CurrentState = progress.State;
            ProcessedCount = progress.ProcessedCount;
            CountdownRemaining = progress.CountdownRemainingSeconds;
            CurrentTps = progress.CurrentTagsPerSecond;

            if (!string.IsNullOrEmpty(progress.CurrentPayload))
            {
                LastPayload = progress.CurrentPayload;
            }

            if (progress.TotalCount > 0)
            {
                ProgressPercent = Math.Clamp((double)progress.ProcessedCount / progress.TotalCount * 100.0, 0, 100);
            }

            UpdateStatusBadge();
        }

        if (Dispatcher.UIThread.CheckAccess())
            UpdateState();
        else
            Dispatcher.UIThread.Post(UpdateState);
    }

    private void OnScanTransmitted(ScanRecord record)
    {
        void UpdateList()
        {
            string entry = $"[{record.Timestamp:HH:mm:ss.fff}] {record.ReaderType}: {record.PrimaryPayload}";
            if (RecentTransmissions.Count > 50)
            {
                RecentTransmissions.RemoveAt(RecentTransmissions.Count - 1);
            }
            RecentTransmissions.Insert(0, entry);
        }

        if (Dispatcher.UIThread.CheckAccess())
            UpdateList();
        else
            Dispatcher.UIThread.Post(UpdateList);
    }

    private void OnErrorOccurred(string error)
    {
        void UpdateError()
        {
            LastPayload = $"Error: {error}";
            CurrentState = SimulationState.Faulted;
            UpdateStatusBadge();
        }

        if (Dispatcher.UIThread.CheckAccess())
            UpdateError();
        else
            Dispatcher.UIThread.Post(UpdateError);
    }

    private void UpdateStatusBadge()
    {
        switch (CurrentState)
        {
            case SimulationState.StartingCountdown:
                StatusBadgeText = $"COUNTDOWN ({CountdownRemaining:F1}s)";
                StatusBadgeColor = "#F59E0B";
                break;
            case SimulationState.Running:
                StatusBadgeText = "ACTIVE STREAM";
                StatusBadgeColor = "#10B981";
                break;
            case SimulationState.Paused:
                StatusBadgeText = "PAUSED";
                StatusBadgeColor = "#3B82F6";
                break;
            case SimulationState.Completed:
                StatusBadgeText = "COMPLETED";
                StatusBadgeColor = "#8B5CF6";
                break;
            case SimulationState.Faulted:
                StatusBadgeText = "FAULT / ERROR";
                StatusBadgeColor = "#EF4444";
                break;
            default:
                StatusBadgeText = "IDLE";
                StatusBadgeColor = "#64748B";
                break;
        }
    }
}

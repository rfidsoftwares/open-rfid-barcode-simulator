using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    public DashboardViewModel Dashboard { get; }
    public GeneratorViewModel Generator { get; }
    public TimingViewModel Timing { get; }
    public InspectorViewModel Inspector { get; }
    public BarcodeStudioViewModel BarcodeStudio { get; }
    public NdefStudioViewModel NdefStudio { get; }
    public ExtendedStudioViewModel ExtendedStudio { get; }
    public TelemetryViewModel Telemetry { get; }

    [ObservableProperty]
    private int _selectedNavIndex = 0;

    [ObservableProperty]
    private bool _isAlwaysOnTop = false;

    public event Action? RequestOpenFloatingHud;

    [RelayCommand]
    public void OpenFloatingHud()
    {
        RequestOpenFloatingHud?.Invoke();
    }

    public MainViewModel(
        DashboardViewModel dashboard,
        GeneratorViewModel generator,
        TimingViewModel timing,
        InspectorViewModel inspector,
        BarcodeStudioViewModel barcodeStudio,
        NdefStudioViewModel ndefStudio,
        ExtendedStudioViewModel extendedStudio,
        TelemetryViewModel telemetry)
    {
        Dashboard = dashboard;
        Generator = generator;
        Timing = timing;
        Inspector = inspector;
        BarcodeStudio = barcodeStudio;
        NdefStudio = ndefStudio;
        ExtendedStudio = extendedStudio;
        Telemetry = telemetry;
        _currentPage = Dashboard;
    }

    [RelayCommand]
    public void Navigate(object? param)
    {
        int index = 0;
        if (param is int i)
        {
            index = i;
        }
        else if (param is string s && int.TryParse(s, out int parsed))
        {
            index = parsed;
        }

        SelectedNavIndex = index;
        CurrentPage = index switch
        {
            0 => Dashboard,
            1 => Generator,
            2 => Timing,
            3 => Inspector,
            4 => BarcodeStudio,
            5 => NdefStudio,
            6 => ExtendedStudio,
            7 => Telemetry,
            _ => Dashboard
        };
    }

    [RelayCommand] public void GoToDashboard() => Navigate(0);
    [RelayCommand] public void GoToGenerator() => Navigate(1);
    [RelayCommand] public void GoToTiming() => Navigate(2);
    [RelayCommand] public void GoToInspector() => Navigate(3);
    [RelayCommand] public void GoToBarcodeStudio() => Navigate(4);
    [RelayCommand] public void GoToNdefStudio() => Navigate(5);
    [RelayCommand] public void GoToExtendedStudio() => Navigate(6);
    [RelayCommand] public void GoToTelemetry() => Navigate(7);
}

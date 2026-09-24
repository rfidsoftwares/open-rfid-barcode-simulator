using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenScanSim.Common.Enums;
using OpenScanSim.Core.Services;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class FloatingHudViewModel : ViewModelBase
{
    public DashboardViewModel Dashboard { get; }

    [ObservableProperty]
    private bool _isExpanded;

    public event Action? RequestShowMainWindow;
    public event Action? RequestCloseHud;

    public FloatingHudViewModel(DashboardViewModel dashboard)
    {
        Dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
    }

    [RelayCommand]
    public void OpenMainWindow()
    {
        RequestShowMainWindow?.Invoke();
    }

    [RelayCommand]
    public void CloseHud()
    {
        RequestCloseHud?.Invoke();
    }
}

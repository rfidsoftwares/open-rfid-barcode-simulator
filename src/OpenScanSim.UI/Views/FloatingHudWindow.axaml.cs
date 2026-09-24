using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using OpenScanSim.Platform.Windows.Native;
using OpenScanSim.UI.ViewModels;

namespace OpenScanSim.UI.Views;

public partial class FloatingHudWindow : Window
{
    public FloatingHudWindow()
    {
        InitializeComponent();

        // Enable drag moving from anywhere inside the HUD
        PointerPressed += (s, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        };

        Opened += OnWindowOpened;
    }

    public FloatingHudWindow(FloatingHudViewModel viewModel) : this()
    {
        DataContext = viewModel;

        viewModel.RequestCloseHud += () => Hide();
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // Position HUD in lower right quadrant
        if (Screens.Primary != null)
        {
            var workingArea = Screens.Primary.WorkingArea;
            int x = workingArea.X + workingArea.Width - (int)Width - 30;
            int y = workingArea.Y + workingArea.Height - (int)Height - 30;
            Position = new PixelPoint(x, y);
        }

        // Apply Win32 WS_EX_NOACTIVATE to prevent stealing keyboard focus on clicks
        if (OperatingSystem.IsWindows())
        {
            var handle = TryGetPlatformHandle()?.Handle;
            if (handle.HasValue && handle.Value != IntPtr.Zero)
            {
                int exStyle = User32.GetWindowLong(handle.Value, User32.GWL_EXSTYLE);
                User32.SetWindowLong(handle.Value, User32.GWL_EXSTYLE, exStyle | User32.WS_EX_NOACTIVATE | User32.WS_EX_TOPMOST);
            }
        }
    }
}

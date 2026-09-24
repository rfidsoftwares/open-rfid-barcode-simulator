using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OpenScanSim.Core.Generators;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Services;
using OpenScanSim.Core.Validators;
using OpenScanSim.Platform.Windows;
using OpenScanSim.UI.ViewModels;
using OpenScanSim.UI.Views;

namespace OpenScanSim.UI;

public partial class App : Application
{
    private bool _isExiting;
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            var hudWindow = Services.GetRequiredService<FloatingHudWindow>();
            var mainVm = Services.GetRequiredService<MainViewModel>();
            var hudVm = Services.GetRequiredService<FloatingHudViewModel>();
            var dashboardVm = Services.GetRequiredService<DashboardViewModel>();
            var orchestrator = Services.GetRequiredService<ISimulationOrchestrator>();
            var hotkeyService = Services.GetRequiredService<Win32GlobalHotkeyService>();

            desktop.MainWindow = mainWindow;

            // Wire HUD open/close events
            hudVm.RequestShowMainWindow += () =>
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            };

            mainVm.RequestOpenFloatingHud += () =>
            {
                hudWindow.Show();
                hudWindow.Activate();
            };

            // Minimize to tray or hide on close
            mainWindow.Closing += (s, e) =>
            {
                if (!_isExiting)
                {
                    e.Cancel = true;
                    mainWindow.Hide();
                }
            };

            hudWindow.Closing += (s, e) =>
            {
                if (!_isExiting)
                {
                    e.Cancel = true;
                    hudWindow.Hide();
                }
            };

            // Configure System Tray Icon
            SetupSystemTray(mainWindow, hudWindow, dashboardVm, desktop);

            // Hook global hotkeys on window handle creation
            mainWindow.Opened += (s, e) =>
            {
                var handle = mainWindow.TryGetPlatformHandle()?.Handle;
                if (handle.HasValue && handle.Value != IntPtr.Zero)
                {
                    hotkeyService.Initialize(handle.Value);
                    
                    // Register F8 (Start/Stop)
                    hotkeyService.RegisterHotkey(1, 0x77, 0, () =>
                    {
                        if (orchestrator.CurrentState == Common.Enums.SimulationState.Running)
                        {
                            dashboardVm.StopSimulationCommand.Execute(null);
                        }
                        else
                        {
                            dashboardVm.StartSimulationCommand.Execute(null);
                        }
                    });

                    // Register F9 (Pause/Resume)
                    hotkeyService.RegisterHotkey(2, 0x78, 0, () =>
                    {
                        if (orchestrator.CurrentState == Common.Enums.SimulationState.Running)
                        {
                            dashboardVm.PauseSimulationCommand.Execute(null);
                        }
                        else if (orchestrator.CurrentState == Common.Enums.SimulationState.Paused)
                        {
                            dashboardVm.ResumeSimulationCommand.Execute(null);
                        }
                    });

                    // Register F10 (Single Step)
                    hotkeyService.RegisterHotkey(3, 0x79, 0, () =>
                    {
                        dashboardVm.TriggerSingleStepCommand.Execute(null);
                    });
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupSystemTray(
        MainWindow mainWindow,
        FloatingHudWindow hudWindow,
        DashboardViewModel dashboardVm,
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var trayIcon = new TrayIcon
            {
                ToolTipText = "OpenRFID & Barcode Simulator - Active"
            };

            // Attempt to load icon from Assets
            try
            {
                var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://OpenScanSim.UI/Assets/logo.png"));
                trayIcon.Icon = new WindowIcon(stream);
            }
            catch
            {
                // Fallback to default if asset stream resolution differs in testing
            }

            var menu = new NativeMenu();

            var itemDashboard = new NativeMenuItem("📊 Show Dashboard");
            itemDashboard.Click += (s, e) =>
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            };
            menu.Add(itemDashboard);

            var itemHud = new NativeMenuItem("🎛️ Show Floating HUD");
            itemHud.Click += (s, e) =>
            {
                hudWindow.Show();
                hudWindow.Activate();
            };
            menu.Add(itemHud);

            menu.Add(new NativeMenuItemSeparator());

            var itemStart = new NativeMenuItem("▶ Start Simulation (F8)");
            itemStart.Click += (s, e) => dashboardVm.StartSimulationCommand.Execute(null);
            menu.Add(itemStart);

            var itemPause = new NativeMenuItem("⏸ Pause / Resume (F9)");
            itemPause.Click += (s, e) =>
            {
                if (dashboardVm.CurrentState == Common.Enums.SimulationState.Running)
                    dashboardVm.PauseSimulationCommand.Execute(null);
                else
                    dashboardVm.ResumeSimulationCommand.Execute(null);
            };
            menu.Add(itemPause);

            var itemStep = new NativeMenuItem("⏭ Single Step (F10)");
            itemStep.Click += (s, e) => dashboardVm.TriggerSingleStepCommand.Execute(null);
            menu.Add(itemStep);

            var itemStop = new NativeMenuItem("⏹ Stop Simulation");
            itemStop.Click += (s, e) => dashboardVm.StopSimulationCommand.Execute(null);
            menu.Add(itemStop);

            menu.Add(new NativeMenuItemSeparator());

            var itemExit = new NativeMenuItem("✕ Exit OpenRFID");
            itemExit.Click += (s, e) =>
            {
                _isExiting = true;
                trayIcon.IsVisible = false;
                desktop.Shutdown();
            };
            menu.Add(itemExit);

            trayIcon.Menu = menu;
            trayIcon.IsVisible = true;

            trayIcon.Clicked += (s, e) =>
            {
                if (mainWindow.IsVisible)
                {
                    mainWindow.Activate();
                }
                else
                {
                    mainWindow.Show();
                    mainWindow.Activate();
                }
            };

            var trayIcons = new TrayIcons { trayIcon };
            TrayIcon.SetIcons(this, trayIcons);
        }
        catch
        {
            // Fallback gracefully on environments without tray support
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IScanDataGenerator, MaskPatternGenerator>();
        services.AddSingleton<IKeyboardSimulator, Win32KeyboardSimulator>();
        services.AddSingleton<IAudioFeedbackService, AudioFeedbackService>();
        services.AddSingleton<ISimulationOrchestrator, SimulationOrchestrator>();
        services.AddSingleton<IProtocolValidator, ProtocolValidator>();
        services.AddSingleton<IChaosNoiseInjector, ChaosNoiseInjector>();
        services.AddSingleton<ITargetProcessTracker, TargetProcessTracker>();
        services.AddSingleton<Win32GlobalHotkeyService>();

        // ViewModels
        services.AddSingleton<GeneratorViewModel>();
        services.AddSingleton<TimingViewModel>();
        services.AddSingleton<InspectorViewModel>();
        services.AddSingleton<BarcodeStudioViewModel>();
        services.AddSingleton<NdefStudioViewModel>();
        services.AddSingleton<ExtendedStudioViewModel>();
        services.AddSingleton<TelemetryViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<FloatingHudViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddSingleton<FloatingHudWindow>();
    }
}

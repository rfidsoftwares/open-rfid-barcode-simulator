using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using OpenScanSim.Common.Constants;
using OpenScanSim.Platform.Windows.Native;

namespace OpenScanSim.Platform.Windows;

/// <summary>
/// System-wide global hotkey manager using Win32 RegisterHotKey.
/// </summary>
public sealed class Win32GlobalHotkeyService : IDisposable
{
    private readonly ConcurrentDictionary<int, Action> _hotkeyActions = new();
    private IntPtr _windowHandle = IntPtr.Zero;
    private ComCtl32.SubclassProc? _subclassProc;
    private const uint SubclassId = 9991;
    private bool _disposed;

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;

        if (OperatingSystem.IsWindows() && _windowHandle != IntPtr.Zero)
        {
            _subclassProc = WndProcSubclass;
            ComCtl32.SetWindowSubclass(_windowHandle, _subclassProc, new UIntPtr(SubclassId), UIntPtr.Zero);
        }
    }

    private IntPtr WndProcSubclass(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData)
    {
        if (uMsg == AppConstants.Win32.WmHotkey)
        {
            ProcessWindowMessage((int)uMsg, wParam);
        }
        return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    /// <summary>
    /// Registers a system-wide global hotkey.
    /// </summary>
    public bool RegisterHotkey(int id, uint virtualKey, uint modifiers, Action action)
    {
        _hotkeyActions[id] = action;

        if (_windowHandle == IntPtr.Zero) return false;

        UnregisterHotkey(id);
        _hotkeyActions[id] = action;

        return User32.RegisterHotKey(_windowHandle, id, modifiers | User32.MOD_NOREPEAT, virtualKey);
    }

    /// <summary>
    /// Unregisters an active hotkey by ID.
    /// </summary>
    public void UnregisterHotkey(int id)
    {
        if (_windowHandle != IntPtr.Zero)
        {
            User32.UnregisterHotKey(_windowHandle, id);
        }
        _hotkeyActions.TryRemove(id, out _);
    }

    /// <summary>
    /// Dispatches hotkey triggers received from the Win32 window message pump (WM_HOTKEY).
    /// </summary>
    public bool ProcessWindowMessage(int msg, IntPtr wParam)
    {
        if (msg == AppConstants.Win32.WmHotkey)
        {
            int id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action.Invoke();
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (OperatingSystem.IsWindows() && _windowHandle != IntPtr.Zero && _subclassProc != null)
            {
                ComCtl32.RemoveWindowSubclass(_windowHandle, _subclassProc, new UIntPtr(SubclassId));
            }

            foreach (int id in _hotkeyActions.Keys)
            {
                UnregisterHotkey(id);
            }
            _disposed = true;
        }
    }
}

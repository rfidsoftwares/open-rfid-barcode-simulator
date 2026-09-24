using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using OpenScanSim.Platform.Windows.Native;

namespace OpenScanSim.Platform.Windows;

public readonly record struct WindowInfo(IntPtr Hwnd, string Title, string ProcessName, uint ProcessId);

public interface ITargetProcessTracker
{
    IReadOnlyList<WindowInfo> GetTopLevelWindows();
    WindowInfo? GetForegroundWindowInfo();
    bool IsTargetWindowFocused(string? targetProcessOrTitle);
}

public sealed class TargetProcessTracker : ITargetProcessTracker
{
    public IReadOnlyList<WindowInfo> GetTopLevelWindows()
    {
        List<WindowInfo> windows = [];

        User32.EnumWindows((hwnd, lParam) =>
        {
            if (!User32.IsWindowVisible(hwnd))
                return true;

            int length = User32.GetWindowTextLength(hwnd);
            if (length == 0)
                return true;

            StringBuilder sb = new(length + 1);
            User32.GetWindowText(hwnd, sb, sb.Capacity);
            string title = sb.ToString().Trim();

            if (string.IsNullOrEmpty(title))
                return true;

            User32.GetWindowThreadProcessId(hwnd, out uint pid);
            string processName = "Unknown";
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
            }
            catch
            {
                // Ignored for system processes
            }

            windows.Add(new WindowInfo(hwnd, title, processName, pid));
            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public WindowInfo? GetForegroundWindowInfo()
    {
        IntPtr fg = User32.GetForegroundWindow();
        if (fg == IntPtr.Zero) return null;

        int length = User32.GetWindowTextLength(fg);
        StringBuilder sb = new(Math.Max(length + 1, 256));
        User32.GetWindowText(fg, sb, sb.Capacity);
        string title = sb.ToString().Trim();

        User32.GetWindowThreadProcessId(fg, out uint pid);
        string processName = "Unknown";
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            processName = proc.ProcessName;
        }
        catch
        {
            // Ignored
        }

        return new WindowInfo(fg, title, processName, pid);
    }

    public bool IsTargetWindowFocused(string? targetProcessOrTitle)
    {
        if (string.IsNullOrWhiteSpace(targetProcessOrTitle))
            return true; // No restriction

        var fg = GetForegroundWindowInfo();
        if (fg == null) return false;

        return fg.Value.Title.Contains(targetProcessOrTitle, StringComparison.OrdinalIgnoreCase) ||
               fg.Value.ProcessName.Contains(targetProcessOrTitle, StringComparison.OrdinalIgnoreCase);
    }
}

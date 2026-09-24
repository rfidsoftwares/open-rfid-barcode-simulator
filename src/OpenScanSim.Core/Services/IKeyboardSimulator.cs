using System.Threading;
using System.Threading.Tasks;
using OpenScanSim.Common.Models;

namespace OpenScanSim.Core.Services;

/// <summary>
/// Abstraction for platform-specific virtual keyboard wedge injection.
/// </summary>
public interface IKeyboardSimulator
{
    /// <summary>
    /// Injects a text payload as virtual keystrokes into the active OS foreground window.
    /// </summary>
    Task SendKeystrokesAsync(string text, WedgeConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a single virtual key with optional modifiers.
    /// </summary>
    Task SendKeyAsync(ushort virtualKey, bool extendedKey = false, CancellationToken cancellationToken = default);
}

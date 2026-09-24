using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Services;
using OpenScanSim.Platform.Windows.Native;

namespace OpenScanSim.Platform.Windows;

/// <summary>
/// Native Windows Virtual Keyboard Wedge simulator using SendInput with KEYEVENTF_UNICODE.
/// </summary>
public sealed class Win32KeyboardSimulator : IKeyboardSimulator
{
    private static readonly char[] AdjacentQwertyKeys = "QWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray();

    public async Task SendKeystrokesAsync(string text, WedgeConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text)) return;

        string fullText = string.Concat(config.Prefix, text, GetTerminatorString(config));

        if (config.Mode == SimulationMode.FastDeviceBurst)
        {
            SendFastBatch(fullText);
        }
        else
        {
            await SendHumanTypingAsync(fullText, config, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task SendKeyAsync(ushort virtualKey, bool extendedKey = false, CancellationToken cancellationToken = default)
    {
        INPUT[] inputs = new INPUT[2];
        uint dwFlags = extendedKey ? User32.KEYEVENTF_EXTENDEDKEY : 0;

        inputs[0] = new INPUT
        {
            type = User32.INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    dwFlags = dwFlags
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = User32.INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    dwFlags = dwFlags | User32.KEYEVENTF_KEYUP
                }
            }
        };

        User32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        return Task.CompletedTask;
    }

    private static void SendFastBatch(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Maximum 2 inputs (down + up) per character
        INPUT[] inputs = new INPUT[text.Length * 2];
        int count = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '\r')
            {
                inputs[count++] = CreateVkInput(0x0D, keyUp: false);
                inputs[count++] = CreateVkInput(0x0D, keyUp: true);
            }
            else if (c == '\n')
            {
                // If preceded by \r, skip standalone \n to avoid double enter
                if (i > 0 && text[i - 1] == '\r')
                {
                    continue;
                }
                inputs[count++] = CreateVkInput(0x0D, keyUp: false);
                inputs[count++] = CreateVkInput(0x0D, keyUp: true);
            }
            else if (c == '\t')
            {
                inputs[count++] = CreateVkInput(0x09, keyUp: false);
                inputs[count++] = CreateVkInput(0x09, keyUp: true);
            }
            else
            {
                inputs[count++] = CreateUnicodeInput(c, keyUp: false);
                inputs[count++] = CreateUnicodeInput(c, keyUp: true);
            }
        }

        if (count > 0)
        {
            User32.SendInput((uint)count, inputs, Marshal.SizeOf<INPUT>());
        }
    }

    private static async Task SendHumanTypingAsync(string text, WedgeConfig config, CancellationToken ct)
    {
        Random rng = Random.Shared;
        int baseSpeed = Math.Max(20, config.HumanTypingSpeedMs);
        int jitter = Math.Max(0, config.HumanJitterMs);

        for (int i = 0; i < text.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            char c = text[i];

            // Typo simulation
            if (config.TypoChancePercent > 0 && rng.NextDouble() * 100 < config.TypoChancePercent && char.IsLetterOrDigit(c))
            {
                char wrongChar = AdjacentQwertyKeys[rng.Next(AdjacentQwertyKeys.Length)];
                SendSingleChar(wrongChar);
                await Task.Delay(rng.Next(150, 300), ct).ConfigureAwait(false);

                // Send Backspace (VK_BACK 0x08)
                SendVk(0x08);
                await Task.Delay(rng.Next(80, 150), ct).ConfigureAwait(false);
            }

            SendSingleChar(c);

            // Gaussian delay calculation: Box-Muller transform
            double u1 = Math.Max(1e-6, rng.NextDouble());
            double u2 = rng.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            int delay = (int)Math.Clamp(baseSpeed + (randStdNormal * jitter), 10, 800);

            await Task.Delay(delay, ct).ConfigureAwait(false);
        }
    }

    private static void SendSingleChar(char c)
    {
        if (c == '\r' || c == '\n')
        {
            SendVk(0x0D);
        }
        else if (c == '\t')
        {
            SendVk(0x09);
        }
        else
        {
            INPUT[] inputs = [CreateUnicodeInput(c, false), CreateUnicodeInput(c, true)];
            User32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
    }

    private static void SendVk(ushort vk)
    {
        INPUT[] inputs = [CreateVkInput(vk, false), CreateVkInput(vk, true)];
        User32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT CreateUnicodeInput(char c, bool keyUp)
    {
        return new INPUT
        {
            type = User32.INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = User32.KEYEVENTF_UNICODE | (keyUp ? User32.KEYEVENTF_KEYUP : 0)
                }
            }
        };
    }

    private static INPUT CreateVkInput(ushort vk, bool keyUp)
    {
        return new INPUT
        {
            type = User32.INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = keyUp ? User32.KEYEVENTF_KEYUP : 0
                }
            }
        };
    }

    private static string GetTerminatorString(WedgeConfig config)
    {
        return config.Terminator switch
        {
            OutputTerminator.Enter => "\r\n",
            OutputTerminator.Tab => "\t",
            OutputTerminator.Space => " ",
            OutputTerminator.Custom => config.CustomTerminator,
            OutputTerminator.None => "",
            _ => "\r\n"
        };
    }
}

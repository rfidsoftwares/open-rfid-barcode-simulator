using System;
using OpenScanSim.Common.Constants;

namespace OpenScanSim.Common.Extensions;

/// <summary>
/// High-performance zero-allocation utilities for Span formatting and CRC calculation.
/// </summary>
public static class SpanExtensions
{
    private static readonly char[] HexLookup = "0123456789ABCDEF".ToCharArray();

    /// <summary>
    /// Calculates CRC-16/CCITT standard (Gen2 RFID / X.25 / CCITT).
    /// </summary>
    public static ushort CalculateCrc16(ReadOnlySpan<byte> data, ushort polynomial = AppConstants.Protocols.Crc16CcittPolynomial, ushort initialValue = AppConstants.Protocols.Crc16Gen2Preset)
    {
        ushort crc = initialValue;

        foreach (byte b in data)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                {
                    crc = (ushort)((crc << 1) ^ polynomial);
                }
                else
                {
                    crc <<= 1;
                }
            }
        }

        return crc;
    }

    /// <summary>
    /// Converts a byte span to uppercase hexadecimal string without intermediate heap allocations.
    /// </summary>
    public static string ToHexStringFast(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return string.Empty;
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Decodes a hexadecimal character span into bytes in-place without heap allocations.
    /// </summary>
    public static bool TryDecodeHex(ReadOnlySpan<char> hex, Span<byte> output, out int bytesWritten)
    {
        bytesWritten = 0;
        if (hex.Length % 2 != 0 || output.Length < hex.Length / 2)
            return false;

        for (int i = 0; i < hex.Length; i += 2)
        {
            int high = GetHexVal(hex[i]);
            int low = GetHexVal(hex[i + 1]);
            if (high < 0 || low < 0)
            {
                bytesWritten = 0;
                return false;
            }

            output[bytesWritten++] = (byte)((high << 4) | low);
        }

        return true;
    }

    private static int GetHexVal(char hex)
    {
        if (hex >= '0' && hex <= '9') return hex - '0';
        if (hex >= 'A' && hex <= 'F') return hex - 'A' + 10;
        if (hex >= 'a' && hex <= 'f') return hex - 'a' + 10;
        return -1;
    }

    /// <summary>
    /// Computes Modulo 10 check digit (EAN-13, UPC-A, GS1 standard).
    /// </summary>
    public static int CalculateModulo10CheckDigit(ReadOnlySpan<char> digits)
    {
        int sum = 0;
        int multiplier = 3;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(digits[i]))
            {
                int val = digits[i] - '0';
                sum += val * multiplier;
                multiplier = multiplier == 3 ? 1 : 3;
            }
        }

        int remainder = sum % 10;
        return remainder == 0 ? 0 : 10 - remainder;
    }

    /// <summary>
    /// Computes Modulo 43 check digit for Code 39.
    /// </summary>
    public static char CalculateModulo43CheckDigit(ReadOnlySpan<char> text)
    {
        const string charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
        int sum = 0;

        foreach (char c in text)
        {
            char upper = char.ToUpperInvariant(c);
            int idx = charset.IndexOf(upper);
            if (idx >= 0)
            {
                sum += idx;
            }
        }

        return charset[sum % 43];
    }

    /// <summary>
    /// Computes ICAO Doc 9303 MRZ Check Digit (7-3-1 weighting algorithm).
    /// </summary>
    public static int CalculateIcaoCheckDigit(ReadOnlySpan<char> input)
    {
        ReadOnlySpan<int> weights = [7, 3, 1];
        int sum = 0;

        for (int i = 0; i < input.Length; i++)
        {
            char c = char.ToUpperInvariant(input[i]);
            int val;

            if (c >= '0' && c <= '9')
            {
                val = c - '0';
            }
            else if (c >= 'A' && c <= 'Z')
            {
                val = c - 'A' + 10;
            }
            else
            {
                val = 0; // '<' filler characters count as 0
            }

            sum += val * weights[i % 3];
        }

        return sum % 10;
    }
}

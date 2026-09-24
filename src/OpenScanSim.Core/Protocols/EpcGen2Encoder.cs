using System;
using OpenScanSim.Common.Constants;
using OpenScanSim.Common.Extensions;

namespace OpenScanSim.Core.Protocols;

/// <summary>
/// UHF Gen2 EPC Tag data encoder and GS1 SGTIN-96 partition table calculator.
/// </summary>
public static class EpcGen2Encoder
{
    // GS1 SGTIN-96 Partition Table definition
    // Partition: [Company Prefix Bits, Company Prefix Digits, Item Reference Bits, Item Reference Digits]
    private static readonly (int PrefixBits, int PrefixDigits, int ItemBits, int ItemDigits)[] PartitionTable =
    [
        (40, 12, 4, 1),  // Partition 0
        (37, 11, 7, 2),  // Partition 1
        (34, 10, 10, 3), // Partition 2
        (30, 9, 14, 4),  // Partition 3
        (27, 8, 17, 5),  // Partition 4
        (24, 7, 20, 6),  // Partition 5
        (20, 6, 24, 7),  // Partition 6
    ];

    /// <summary>
    /// Calculates the 16-bit Protocol Control (PC) word.
    /// For a standard 96-bit (6 words) EPC: 0x3000 (bits: length=6, UMI=0, XI=0, ISO=0).
    /// </summary>
    public static string CalculatePcWord(int epcBitLength = 96, bool toggleIso = false)
    {
        int words = epcBitLength / 16;
        int pc = (words & 0x1F) << 11;
        if (toggleIso) pc |= 0x0200;
        return pc.ToString("X4");
    }

    /// <summary>
    /// Generates a valid 96-bit GS1 SGTIN-96 Hex EPC string from components.
    /// </summary>
    public static string EncodeSgtin96(int filterValue, int partition, ulong companyPrefix, ulong itemReference, ulong serialNumber)
    {
        if (partition < 0 || partition > 6)
            throw new ArgumentOutOfRangeException(nameof(partition), "Partition must be between 0 and 6.");

        var part = PartitionTable[partition];

        // 96-bit binary layout:
        // [8 bits Header 0x30] [3 bits Filter] [3 bits Partition] [M bits CompanyPrefix] [N bits ItemRef] [38 bits Serial]
        Span<byte> bits = stackalloc byte[12]; // 96 bits = 12 bytes

        // Write Header 0x30 (SGTIN-96 standard header)
        bits[0] = 0x30;

        // Bit position pointer (starts after 8-bit header)
        int bitPos = 8;

        WriteBits(bits, ref bitPos, (ulong)(filterValue & 0x07), 3);
        WriteBits(bits, ref bitPos, (ulong)(partition & 0x07), 3);
        WriteBits(bits, ref bitPos, companyPrefix, part.PrefixBits);
        WriteBits(bits, ref bitPos, itemReference, part.ItemBits);
        WriteBits(bits, ref bitPos, serialNumber & 0x3FFFFFFFFF, 38);

        return SpanExtensions.ToHexStringFast(bits);
    }

    /// <summary>
    /// Computes the 16-bit CRC over PC word + EPC hex bytes without heap allocations.
    /// </summary>
    public static string CalculateEpcCrc16(string pcWordHex, string epcHex)
    {
        Span<char> combined = stackalloc char[pcWordHex.Length + epcHex.Length];
        pcWordHex.AsSpan().CopyTo(combined);
        epcHex.AsSpan().CopyTo(combined[pcWordHex.Length..]);

        Span<byte> data = stackalloc byte[combined.Length / 2];
        if (SpanExtensions.TryDecodeHex(combined, data, out int bytesWritten))
        {
            ushort crc = SpanExtensions.CalculateCrc16(data[..bytesWritten]);
            return crc.ToString("X4");
        }

        return "0000";
    }

    private static void WriteBits(Span<byte> buffer, ref int bitPos, ulong value, int bitCount)
    {
        for (int i = bitCount - 1; i >= 0; i--)
        {
            int bit = (int)((value >> i) & 1);
            int byteIndex = bitPos / 8;
            int bitIndexInByte = 7 - (bitPos % 8);

            if (bit == 1)
            {
                buffer[byteIndex] |= (byte)(1 << bitIndexInByte);
            }
            else
            {
                buffer[byteIndex] &= (byte)~(1 << bitIndexInByte);
            }

            bitPos++;
        }
    }
}

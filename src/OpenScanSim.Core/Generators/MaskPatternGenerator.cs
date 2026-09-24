using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using OpenScanSim.Common.Constants;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Protocols;

namespace OpenScanSim.Core.Generators;

/// <summary>
/// High-performance template mask pattern generator.
/// Evaluates expressions like "3034{HEX:8}{SEQ:8}" on-the-fly without memory allocation.
/// </summary>
public sealed class MaskPatternGenerator : IScanDataGenerator
{
    private static readonly char[] HexChars = "0123456789ABCDEF".ToCharArray();
    private static readonly char[] AlphaNumChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] NumChars = "0123456789".ToCharArray();

    public async IAsyncEnumerable<ScanRecord> GenerateStreamAsync(
        GeneratorConfig config,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Random rng = config.Seed.HasValue ? new Random(config.Seed.Value) : Random.Shared;
        ulong total = config.TotalCount;
        ulong startSeq = config.StartSequence;

        // Check if direct pasted list mode is active
        if (!string.IsNullOrWhiteSpace(config.DirectPastedList))
        {
            string[] items = config.DirectPastedList.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (items.Length > 0)
            {
                ulong limit = total > 0 ? total : (ulong)items.Length;

                for (ulong i = 0; i < limit; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ulong currentSeq = startSeq + i;
                    string rawPayload = items[(int)(i % (ulong)items.Length)];
                    string finalPayload = rawPayload;

                    if (config.ReaderType == ReaderType.Barcode1D2D)
                    {
                        finalPayload = BarcodeSymbologyEncoder.FormatBarcode(
                            config.Symbology,
                            rawPayload,
                            config.IncludeCheckDigit,
                            config.IncludeAimPrefix);
                    }

                    string? tid = config.IncludeTid && config.ReaderType == ReaderType.UhfRfid
                        ? "E28011302000" + (currentSeq & 0xFFFFFF).ToString("X6")
                        : null;

                    yield return new ScanRecord(
                        ReaderType: config.ReaderType,
                        PrimaryPayload: finalPayload,
                        ProtocolControlWord: config.ReaderType == ReaderType.UhfRfid ? AppConstants.Protocols.DefaultPcWord96Bit : null,
                        Tid: tid,
                        UserMemoryHex: config.IncludeUserMemory ? "0000000000000000" : null,
                        Timestamp: DateTime.UtcNow,
                        SequenceIndex: currentSeq);
                }

                yield break;
            }
        }

        string pattern = string.IsNullOrWhiteSpace(config.MaskPattern)
            ? GetDefaultMask(config.ReaderType)
            : config.MaskPattern;

        for (ulong i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ulong currentSeq = startSeq + i;
            string rawPayload = EvaluatePattern(pattern, currentSeq, rng);
            string finalPayload = rawPayload;

            if (config.ReaderType == ReaderType.Barcode1D2D)
            {
                finalPayload = BarcodeSymbologyEncoder.FormatBarcode(
                    config.Symbology,
                    rawPayload,
                    config.IncludeCheckDigit,
                    config.IncludeAimPrefix);
            }

            string? tid = config.IncludeTid && config.ReaderType == ReaderType.UhfRfid
                ? "E28011302000" + (currentSeq & 0xFFFFFF).ToString("X6")
                : null;

            yield return new ScanRecord(
                ReaderType: config.ReaderType,
                PrimaryPayload: finalPayload,
                ProtocolControlWord: config.ReaderType == ReaderType.UhfRfid ? AppConstants.Protocols.DefaultPcWord96Bit : null,
                Tid: tid,
                UserMemoryHex: config.IncludeUserMemory ? "0000000000000000" : null,
                Timestamp: DateTime.UtcNow,
                SequenceIndex: currentSeq);
        }
    }

    private static string GetDefaultMask(ReaderType readerType)
    {
        return readerType switch
        {
            ReaderType.Barcode1D2D => "400638133393",
            ReaderType.HfRfid => "04{HEX:12}",
            ReaderType.Nfc => "https://rfidsoftwares.com/tag/{SEQ:6}",
            ReaderType.OcrPassport => "P<INDSHARMA<<AKASH<<<<<<<<<<<<<<<<<<<<<<<<<<",
            ReaderType.ScaleWedge => "ST,GS,+024.500 KG",
            ReaderType.BleBeacon => "0201061AFF4C000215E2C56DB5DFFB48D2B060D0F5A71096E000640001C5",
            _ => AppConstants.Defaults.DefaultUhfMask
        };
    }

    /// <summary>
    /// Evaluates dynamic tokens in the pattern string for a given sequence number.
    /// </summary>
    public static string EvaluatePattern(string pattern, ulong sequence, Random rng)
    {
        StringBuilder sb = new(pattern.Length + 32);
        ReadOnlySpan<char> span = pattern.AsSpan();
        int idx = 0;

        while (idx < span.Length)
        {
            if (span[idx] == '{')
            {
                int closeIdx = span[idx..].IndexOf('}');
                if (closeIdx > 0)
                {
                    ReadOnlySpan<char> token = span.Slice(idx + 1, closeIdx - 1);
                    ProcessToken(token, sequence, rng, sb);
                    idx += closeIdx + 1;
                    continue;
                }
            }

            char c = span[idx];
            switch (c)
            {
                case '#':
                    sb.Append(NumChars[rng.Next(NumChars.Length)]);
                    break;
                case 'X':
                    sb.Append(AlphaNumChars[rng.Next(AlphaNumChars.Length)]);
                    break;
                case 'H':
                    sb.Append(HexChars[rng.Next(HexChars.Length)]);
                    break;
                default:
                    sb.Append(c);
                    break;
            }

            idx++;
        }

        return sb.ToString();
    }

    private static void ProcessToken(ReadOnlySpan<char> token, ulong sequence, Random rng, StringBuilder sb)
    {
        if (token.StartsWith("HEX:", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(token[4..], out int len) && len > 0)
            {
                for (int j = 0; j < len; j++)
                {
                    sb.Append(HexChars[rng.Next(HexChars.Length)]);
                }
                return;
            }
        }
        else if (token.StartsWith("SEQ:", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(token[4..], out int digits) && digits > 0)
            {
                string format = "D" + digits;
                sb.Append(sequence.ToString(format));
                return;
            }
        }
        else if (token.Equals("SEQ", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append(sequence);
            return;
        }
        else if (token.Equals("UUID", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append(Guid.NewGuid().ToString("N").ToUpperInvariant());
            return;
        }
        else if (token.StartsWith("DATE:", StringComparison.OrdinalIgnoreCase))
        {
            string dateFormat = token[5..].ToString();
            sb.Append(DateTime.UtcNow.ToString(dateFormat));
            return;
        }

        // Fallback: output literal
        sb.Append('{').Append(token).Append('}');
    }
}

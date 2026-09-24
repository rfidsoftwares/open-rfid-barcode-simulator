using System;
using OpenScanSim.Common.Enums;

namespace OpenScanSim.Common.Models;

/// <summary>
/// Immutable scan payload record delivered by generators.
/// </summary>
public readonly record struct ScanRecord(
    ReaderType ReaderType,
    string PrimaryPayload,
    string? ProtocolControlWord = null,
    string? Tid = null,
    string? UserMemoryHex = null,
    string? ReservedMemoryHex = null,
    DateTime? Timestamp = null,
    ulong SequenceIndex = 0,
    double? RssiDbm = null,
    int? AntennaPort = null,
    ushort? Crc16 = null,
    bool IsFault = false,
    string? FaultDescription = null)
{
    public DateTime EffectiveTimestamp => Timestamp ?? DateTime.UtcNow;

    /// <summary>
    /// Formats payload into wedge transmission string based on selected options.
    /// </summary>
    public string ToWedgeOutput(bool includeTid = false, bool includeUser = false, string delimiter = "|")
    {
        if (!includeTid && !includeUser)
        {
            return PrimaryPayload;
        }

        int requiredLen = PrimaryPayload.Length
            + (includeTid && !string.IsNullOrEmpty(Tid) ? delimiter.Length + Tid.Length : 0)
            + (includeUser && !string.IsNullOrEmpty(UserMemoryHex) ? delimiter.Length + UserMemoryHex.Length : 0);

        if (requiredLen > 512)
        {
            var sb = new System.Text.StringBuilder(requiredLen);
            sb.Append(PrimaryPayload);
            if (includeTid && !string.IsNullOrEmpty(Tid)) sb.Append(delimiter).Append(Tid);
            if (includeUser && !string.IsNullOrEmpty(UserMemoryHex)) sb.Append(delimiter).Append(UserMemoryHex);
            return sb.ToString();
        }

        Span<char> buffer = stackalloc char[512];
        int pos = 0;

        PrimaryPayload.AsSpan().CopyTo(buffer[pos..]);
        pos += PrimaryPayload.Length;

        if (includeTid && !string.IsNullOrEmpty(Tid))
        {
            delimiter.AsSpan().CopyTo(buffer[pos..]);
            pos += delimiter.Length;
            Tid.AsSpan().CopyTo(buffer[pos..]);
            pos += Tid.Length;
        }

        if (includeUser && !string.IsNullOrEmpty(UserMemoryHex))
        {
            delimiter.AsSpan().CopyTo(buffer[pos..]);
            pos += delimiter.Length;
            UserMemoryHex.AsSpan().CopyTo(buffer[pos..]);
            pos += UserMemoryHex.Length;
        }

        return new string(buffer[..pos]);
    }
}

/// <summary>
/// Configuration parameters for scan data generators.
/// </summary>
public sealed class GeneratorConfig
{
    public ReaderType ReaderType { get; set; } = ReaderType.UhfRfid;
    public string MaskPattern { get; set; } = "3034{HEX:8}{SEQ:8}";
    public ulong StartSequence { get; set; } = 1;
    public ulong TotalCount { get; set; } = 1_000_000;
    public BarcodeSymbology Symbology { get; set; } = BarcodeSymbology.Code128;
    public bool IncludeCheckDigit { get; set; } = true;
    public bool IncludeAimPrefix { get; set; } = false;
    public UidByteOrder UidByteOrder { get; set; } = UidByteOrder.MsbBigEndian;
    public string? SourceFilePath { get; set; }
    public bool IncludeTid { get; set; } = true;
    public bool IncludeUserMemory { get; set; } = false;
    public int? Seed { get; set; }
    public string? DirectPastedList { get; set; }
}

/// <summary>
/// Timing and scheduling options.
/// </summary>
public sealed class TimingConfig
{
    public double StartCountdownSeconds { get; set; } = 5.0;
    public double IntervalSeconds { get; set; } = 2.5;
    public int BurstBatchSize { get; set; } = 1;
    public bool LoopContinuously { get; set; } = false;
}

/// <summary>
/// Keystroke output wedge configuration.
/// </summary>
public sealed class WedgeConfig
{
    public SimulationMode Mode { get; set; } = SimulationMode.FastDeviceBurst;
    public int HumanTypingSpeedMs { get; set; } = 110;
    public int HumanJitterMs { get; set; } = 35;
    public double TypoChancePercent { get; set; } = 0.0;
    public string Prefix { get; set; } = "";
    public OutputTerminator Terminator { get; set; } = OutputTerminator.Enter;
    public string CustomTerminator { get; set; } = "\r\n";
    public bool AudioFeedbackEnabled { get; set; } = true;
    public int AudioPitchHz { get; set; } = 2400;
    public int AudioDurationMs { get; set; } = 60;
}

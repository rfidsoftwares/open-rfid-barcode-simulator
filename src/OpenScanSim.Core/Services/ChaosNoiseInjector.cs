using System;
using System.Security.Cryptography;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;

namespace OpenScanSim.Core.Services;

/// <summary>
/// Hardware chaos and noise injection service for simulating real-world scanner faults.
/// </summary>
public interface IChaosNoiseInjector
{
    ScanRecord ApplyChaos(ScanRecord record, double chaosRatePercent, ChaosFaultMode faultMode);
}

public sealed class ChaosNoiseInjector : IChaosNoiseInjector
{
    public ScanRecord ApplyChaos(ScanRecord record, double chaosRatePercent, ChaosFaultMode faultMode)
    {
        if (chaosRatePercent <= 0.0 || faultMode == ChaosFaultMode.None)
            return record;

        double roll = RandomNumberGenerator.GetInt32(0, 10000) / 100.0;
        if (roll > chaosRatePercent)
            return record;

        return faultMode switch
        {
            ChaosFaultMode.NoReadString =>
                record with { PrimaryPayload = "NOREAD", IsFault = true, FaultDescription = "Simulated Hardware Optical Read Failure (NOREAD)" },

            ChaosFaultMode.TruncatePayload =>
                record with
                {
                    PrimaryPayload = record.PrimaryPayload.Length > 4 ? record.PrimaryPayload[..(record.PrimaryPayload.Length / 2)] : record.PrimaryPayload,
                    IsFault = true,
                    FaultDescription = "Simulated Partial Buffer Truncation"
                },

            ChaosFaultMode.CorruptCrc =>
                record with
                {
                    Crc16 = (ushort)(record.Crc16.HasValue ? record.Crc16.Value ^ 0xFFFF : 0xDEAD),
                    IsFault = true,
                    FaultDescription = "Simulated Bit-Flip / Corrupted Gen2 CRC-16"
                },

            ChaosFaultMode.DuplicateBurstScan =>
                record with
                {
                    IsFault = true,
                    FaultDescription = "Simulated Tag Multipath / Duplicate Fast Reflection"
                },

            _ => record
        };
    }
}

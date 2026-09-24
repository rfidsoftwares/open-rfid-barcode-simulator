using System;
using System.Globalization;
using OpenScanSim.Common.Enums;

namespace OpenScanSim.Core.Protocols;

/// <summary>
/// Digital industrial scale serial and keyboard wedge stream protocol encoder.
/// </summary>
public static class DigitalScaleWedgeEncoder
{
    /// <summary>
    /// Formats a weight reading into the specified industrial scale protocol string.
    /// </summary>
    public static string FormatScaleReading(
        DigitalScaleProtocol protocol,
        decimal weight,
        string unit = "kg",
        bool isStable = true,
        bool isGross = true,
        decimal tareWeight = 0.00m)
    {
        unit = unit.ToLowerInvariant() == "lb" ? "lb" : "kg";
        string sign = weight >= 0 ? " " : "-";
        decimal absWeight = Math.Abs(weight);

        return protocol switch
        {
            DigitalScaleProtocol.MettlerToledoContinuous =>
                FormatMettlerToledo(absWeight, tareWeight, isStable, isGross, unit),

            DigitalScaleProtocol.CasStandard =>
                FormatCas(weight, unit, isStable, isGross),

            DigitalScaleProtocol.AveryBerkel =>
                FormatAveryBerkel(weight, unit, isStable),

            DigitalScaleProtocol.NciGeneral =>
                FormatNci(weight, unit, isStable),

            _ => $"{weight:F2} {unit}"
        };
    }

    private static string FormatMettlerToledo(decimal weight, decimal tare, bool stable, bool gross, string unit)
    {
        char statusA = '0'; // standard resolution
        char statusB = stable ? '0' : '1'; // 0 = stable, 1 = motion
        char statusC = gross ? '0' : '1'; // 0 = gross, 1 = net

        string weightStr = weight.ToString("0000.00", CultureInfo.InvariantCulture);
        string tareStr = tare.ToString("0000.00", CultureInfo.InvariantCulture);

        return $"\x02{statusA}{statusB}{statusC}{weightStr}{tareStr}\r";
    }

    private static string FormatCas(decimal weight, string unit, bool stable, bool gross)
    {
        string status = stable ? "ST" : "US"; // Stable vs Unstable
        string type = gross ? "GS" : "NT";   // Gross vs Net
        string weightFormatted = weight.ToString("+000.000;-000.000;+000.000", CultureInfo.InvariantCulture);
        string unitFormatted = unit.ToUpperInvariant().PadRight(2);

        return $"{status},{type},{weightFormatted} {unitFormatted}\r\n";
    }

    private static string FormatAveryBerkel(decimal weight, string unit, bool stable)
    {
        string status = stable ? "S" : "M";
        string weightFormatted = weight.ToString(" 000.00;-000.00; 000.00", CultureInfo.InvariantCulture);
        return $"{weightFormatted} {unit} {status}";
    }

    private static string FormatNci(decimal weight, string unit, bool stable)
    {
        string status = stable ? "S" : "M";
        string weightFormatted = weight.ToString("000.00", CultureInfo.InvariantCulture).PadLeft(7);
        return $"{status}{weightFormatted}{unit.ToUpperInvariant()}";
    }
}

using System;
using System.Text;
using OpenScanSim.Common.Constants;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Extensions;

namespace OpenScanSim.Core.Protocols;

/// <summary>
/// Barcode symbology formatting, AIM prefixing, and check digit generator.
/// </summary>
public static class BarcodeSymbologyEncoder
{
    /// <summary>
    /// Formats payload according to symbology requirements (AIM prefix, check digits, FNC1).
    /// </summary>
    public static string FormatBarcode(
        BarcodeSymbology symbology,
        string rawData,
        bool includeCheckDigit = true,
        bool includeAimPrefix = false)
    {
        string body = rawData;

        switch (symbology)
        {
            case BarcodeSymbology.Ean13:
                body = FormatEan13(rawData);
                break;
            case BarcodeSymbology.UpcA:
                body = FormatUpcA(rawData);
                break;
            case BarcodeSymbology.Code39 when includeCheckDigit:
                body = rawData + SpanExtensions.CalculateModulo43CheckDigit(rawData.AsSpan());
                break;
        }

        if (!includeAimPrefix)
        {
            return body;
        }

        string aimPrefix = symbology switch
        {
            BarcodeSymbology.Code128 => AppConstants.Protocols.AimCodeGs1128,
            BarcodeSymbology.QrCode => AppConstants.Protocols.AimCodeQrCode,
            BarcodeSymbology.DataMatrix => AppConstants.Protocols.AimCodeDataMatrix,
            _ => string.Empty
        };

        return aimPrefix + body;
    }

    /// <summary>
    /// Formats 12-digit number into valid 13-digit EAN-13 string with Modulo 10 check digit.
    /// </summary>
    public static string FormatEan13(string digits12)
    {
        ReadOnlySpan<char> cleaned = digits12.AsSpan().Trim();
        if (cleaned.Length >= 13)
        {
            cleaned = cleaned[..12];
        }

        int check = SpanExtensions.CalculateModulo10CheckDigit(cleaned);
        return string.Concat(cleaned, check.ToString());
    }

    /// <summary>
    /// Formats 11-digit number into valid 12-digit UPC-A string with Modulo 10 check digit.
    /// </summary>
    public static string FormatUpcA(string digits11)
    {
        ReadOnlySpan<char> cleaned = digits11.AsSpan().Trim();
        if (cleaned.Length >= 12)
        {
            cleaned = cleaned[..11];
        }

        int check = SpanExtensions.CalculateModulo10CheckDigit(cleaned);
        return string.Concat(cleaned, check.ToString());
    }
}

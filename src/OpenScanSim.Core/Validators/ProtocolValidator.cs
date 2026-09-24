using System;
using System.Text.RegularExpressions;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Extensions;

namespace OpenScanSim.Core.Validators;

public readonly record struct ValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string message) => new(false, message);
}

public interface IProtocolValidator
{
    ValidationResult Validate(ReaderType readerType, string payload, BarcodeSymbology? symbology = null);
}

public sealed class ProtocolValidator : IProtocolValidator
{
    private static readonly Regex HexRegex = new(@"\A\b[0-9a-fA-F]+\b\Z", RegexOptions.Compiled);

    public ValidationResult Validate(ReaderType readerType, string payload, BarcodeSymbology? symbology = null)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return ValidationResult.Failure("Payload cannot be empty.");

        return readerType switch
        {
            ReaderType.UhfRfid => ValidateUhfEpc(payload),
            ReaderType.Barcode1D2D => ValidateBarcode(payload, symbology ?? BarcodeSymbology.Code128),
            ReaderType.HfRfid or ReaderType.Nfc => ValidateHfUid(payload),
            _ => ValidationResult.Success()
        };
    }

    private static ValidationResult ValidateUhfEpc(string epcHex)
    {
        string cleaned = epcHex.Trim();
        if (!HexRegex.IsMatch(cleaned))
            return ValidationResult.Failure("UHF EPC must be valid hexadecimal characters.");

        if (cleaned.Length != 24 && cleaned.Length != 32 && cleaned.Length != 64)
            return ValidationResult.Failure($"Invalid EPC hex length ({cleaned.Length} chars). Standard EPC lengths are 24 (96-bit), 32 (128-bit), or 64 (256-bit).");

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateBarcode(string barcode, BarcodeSymbology symbology)
    {
        string cleaned = barcode.Trim();

        switch (symbology)
        {
            case BarcodeSymbology.Ean13:
                if (cleaned.Length != 13 || !ulong.TryParse(cleaned, out _))
                    return ValidationResult.Failure("EAN-13 must be exactly 13 digits.");
                int expectedEanCheck = SpanExtensions.CalculateModulo10CheckDigit(cleaned.AsSpan()[..12]);
                int actualEanCheck = cleaned[12] - '0';
                if (expectedEanCheck != actualEanCheck)
                    return ValidationResult.Failure($"Invalid EAN-13 Check Digit: expected {expectedEanCheck}, found {actualEanCheck}.");
                break;

            case BarcodeSymbology.UpcA:
                if (cleaned.Length != 12 || !ulong.TryParse(cleaned, out _))
                    return ValidationResult.Failure("UPC-A must be exactly 12 digits.");
                int expectedUpcCheck = SpanExtensions.CalculateModulo10CheckDigit(cleaned.AsSpan()[..11]);
                int actualUpcCheck = cleaned[11] - '0';
                if (expectedUpcCheck != actualUpcCheck)
                    return ValidationResult.Failure($"Invalid UPC-A Check Digit: expected {expectedUpcCheck}, found {actualUpcCheck}.");
                break;
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateHfUid(string uidHex)
    {
        string cleaned = uidHex.Replace(":", "").Replace("-", "").Trim();
        if (!HexRegex.IsMatch(cleaned))
            return ValidationResult.Failure("UID must contain valid hex characters.");

        if (cleaned.Length != 8 && cleaned.Length != 14 && cleaned.Length != 16)
            return ValidationResult.Failure($"Invalid UID length ({cleaned.Length} chars). Standard sizes are 4-byte (8 hex), 7-byte (14 hex), or 8-byte (16 hex).");

        return ValidationResult.Success();
    }
}

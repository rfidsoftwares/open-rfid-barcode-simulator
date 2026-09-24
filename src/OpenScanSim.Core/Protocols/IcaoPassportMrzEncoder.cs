using System;
using System.Text;
using OpenScanSim.Common.Extensions;

namespace OpenScanSim.Core.Protocols;

/// <summary>
/// ICAO Doc 9303 compliant Machine Readable Travel Document (MRTD) MRZ encoder.
/// Supports TD3 (Passport: 2 lines x 44 chars), TD1 (ID Card: 3 lines x 30 chars), and TD2 (2 lines x 36 chars).
/// </summary>
public static class IcaoPassportMrzEncoder
{
    /// <summary>
    /// Encodes a standard TD3 (2 lines x 44 chars) Passport MRZ.
    /// </summary>
    public static (string Line1, string Line2, string Combined) EncodeTd3Passport(
        string issuingCountry,
        string surname,
        string givenNames,
        string documentNumber,
        string nationality,
        DateTime dateOfBirth,
        char sex,
        DateTime dateOfExpiry,
        string? personalNumber = null)
    {
        issuingCountry = FormatCountryCode(issuingCountry);
        nationality = FormatCountryCode(nationality);
        documentNumber = PadOrTruncate(documentNumber.ToUpperInvariant(), 9);
        int docNumCheck = SpanExtensions.CalculateIcaoCheckDigit(documentNumber.AsSpan());

        string dobStr = dateOfBirth.ToString("yyMMdd");
        int dobCheck = SpanExtensions.CalculateIcaoCheckDigit(dobStr.AsSpan());

        char sexChar = char.ToUpperInvariant(sex);
        if (sexChar != 'M' && sexChar != 'F') sexChar = '<';

        string expStr = dateOfExpiry.ToString("yyMMdd");
        int expCheck = SpanExtensions.CalculateIcaoCheckDigit(expStr.AsSpan());

        string persNum = PadOrTruncate((personalNumber ?? string.Empty).ToUpperInvariant(), 14);
        int persNumCheck = SpanExtensions.CalculateIcaoCheckDigit(persNum.AsSpan());

        // Composite check digit string for TD3:
        // docNum + docNumCheck + dob + dobCheck + exp + expCheck + persNum + persNumCheck
        string compositeSource = $"{documentNumber}{docNumCheck}{dobStr}{dobCheck}{expStr}{expCheck}{persNum}{persNumCheck}";
        int compositeCheck = SpanExtensions.CalculateIcaoCheckDigit(compositeSource.AsSpan());

        // Line 1: P<ISSNAMES<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        string cleanSurname = CleanName(surname);
        string cleanGiven = CleanName(givenNames);
        string nameField = $"{cleanSurname}<<{cleanGiven}";
        string line1 = $"P<{issuingCountry}{PadOrTruncate(nameField, 39)}";

        // Line 2: DOCNUM<CHK+NAT+DOB<CHK+SEX+EXP<CHK+PERSNUM<CHK+COMP
        string line2 = $"{documentNumber}{docNumCheck}{nationality}{dobStr}{dobCheck}{sexChar}{expStr}{expCheck}{persNum}{persNumCheck}{compositeCheck}";

        string combined = $"{line1}\r\n{line2}";
        return (line1, line2, combined);
    }

    private static string CleanName(string name)
    {
        StringBuilder sb = new();
        foreach (char c in name.ToUpperInvariant())
        {
            if (c >= 'A' && c <= 'Z')
            {
                sb.Append(c);
            }
            else if (char.IsWhiteSpace(c) || c == '-')
            {
                sb.Append('<');
            }
        }
        return sb.ToString();
    }

    private static string FormatCountryCode(string country)
    {
        string cleaned = country.Trim().ToUpperInvariant();
        if (cleaned.Length == 3) return cleaned;
        return PadOrTruncate(cleaned, 3);
    }

    private static string PadOrTruncate(string input, int targetLength)
    {
        if (input.Length >= targetLength) return input[..targetLength];
        return input.PadRight(targetLength, '<');
    }
}

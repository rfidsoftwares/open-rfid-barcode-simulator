using System;
using System.IO;
using System.Text;
using OpenScanSim.Common.Extensions;

namespace OpenScanSim.Core.Protocols;

/// <summary>
/// NFC Forum NDEF (NFC Data Exchange Format) message and record binary builder.
/// </summary>
public static class NdefMessageEncoder
{
    private static readonly (string Prefix, byte Code)[] UriPrefixLookup =
    [
        ("http://www.", 0x01),
        ("https://www.", 0x02),
        ("http://", 0x03),
        ("https://", 0x04),
        ("tel:", 0x05),
        ("mailto:", 0x06)
    ];

    /// <summary>
    /// Creates a complete binary NDEF Well-Known URI Record.
    /// </summary>
    public static byte[] CreateUriRecord(string url)
    {
        byte prefixCode = 0x00;
        string remainder = url;

        foreach (var (prefix, code) in UriPrefixLookup)
        {
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                prefixCode = code;
                remainder = url[prefix.Length..];
                break;
            }
        }

        byte[] payloadBytes = Encoding.UTF8.GetBytes(remainder);
        byte[] record = new byte[5 + payloadBytes.Length];

        // NDEF Header flags: MB=1 (Message Begin), ME=1 (Message End), SR=1 (Short Record), TNF=0x01 (Well-Known)
        record[0] = 0xD1; // 1101 0001
        record[1] = 0x01; // Type Length = 1
        record[2] = (byte)(payloadBytes.Length + 1); // Payload Length
        record[3] = (byte)'U'; // Record Type 'U' (URI)
        record[4] = prefixCode; // URI Identifier Code

        payloadBytes.CopyTo(record.AsSpan(5));
        return record;
    }

    /// <summary>
    /// Creates a complete binary NDEF Well-Known Text Record with language code (ISO 639-1).
    /// </summary>
    public static byte[] CreateTextRecord(string text, string languageCode = "en")
    {
        byte[] langBytes = Encoding.ASCII.GetBytes(languageCode);
        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        byte statusByte = (byte)(langBytes.Length & 0x3F); // UTF-8 encoding (bit 7 = 0) + lang length

        int payloadLen = 1 + langBytes.Length + textBytes.Length;
        byte[] record = new byte[4 + payloadLen];

        record[0] = 0xD1; // MB=1, ME=1, SR=1, TNF=0x01
        record[1] = 0x01; // Type Length = 1 ('T')
        record[2] = (byte)payloadLen; // Payload Length
        record[3] = (byte)'T'; // Record Type 'T' (Text)
        record[4] = statusByte;

        langBytes.CopyTo(record.AsSpan(5));
        textBytes.CopyTo(record.AsSpan(5 + langBytes.Length));

        return record;
    }

    /// <summary>
    /// Converts NDEF binary record to uppercase hex string.
    /// </summary>
    public static string ToHexPayload(byte[] ndefRecord)
    {
        return SpanExtensions.ToHexStringFast(ndefRecord);
    }
}

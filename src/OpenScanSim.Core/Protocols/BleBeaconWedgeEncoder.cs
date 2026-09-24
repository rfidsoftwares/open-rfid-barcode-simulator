using System;
using System.Text;
using OpenScanSim.Common.Extensions;

namespace OpenScanSim.Core.Protocols;

/// <summary>
/// BLE Beacon advertisement frame encoder (Apple iBeacon, Eddystone-UID, Eddystone-URL).
/// </summary>
public static class BleBeaconWedgeEncoder
{
    /// <summary>
    /// Encodes standard Apple iBeacon raw advertisement PDU payload (30 bytes).
    /// </summary>
    public static (byte[] RawBytes, string HexPayload) EncodeIBeacon(
        Guid proximityUuid,
        ushort major,
        ushort minor,
        sbyte measuredPowerRssiAt1M = -59)
    {
        byte[] payload = new byte[30];
        // Flags
        payload[0] = 0x02; // Length 2
        payload[1] = 0x01; // Flags data type
        payload[2] = 0x06; // LE General Discoverable Mode, BR/EDR Not Supported

        // Manufacturer Specific Data
        payload[3] = 0x1A; // Length 26
        payload[4] = 0xFF; // Manufacturer Specific
        payload[5] = 0x4C; // Apple Inc Company ID Low
        payload[6] = 0x00; // Apple Inc Company ID High
        payload[7] = 0x02; // iBeacon Type
        payload[8] = 0x15; // iBeacon Data Length (21 bytes)

        // Proximity UUID (16 bytes Big Endian)
        byte[] uuidBytes = proximityUuid.ToByteArray();
        // Convert to Big-Endian network order
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(uuidBytes, 0, 4);
            Array.Reverse(uuidBytes, 4, 2);
            Array.Reverse(uuidBytes, 6, 2);
        }
        Array.Copy(uuidBytes, 0, payload, 9, 16);

        // Major (2 bytes Big Endian)
        payload[25] = (byte)(major >> 8);
        payload[26] = (byte)(major & 0xFF);

        // Minor (2 bytes Big Endian)
        payload[27] = (byte)(minor >> 8);
        payload[28] = (byte)(minor & 0xFF);

        // Measured Power RSSI at 1 meter
        payload[29] = (byte)measuredPowerRssiAt1M;

        string hex = SpanExtensions.ToHexStringFast(payload);
        return (payload, hex);
    }

    /// <summary>
    /// Encodes Eddystone-URL broadcast frame.
    /// </summary>
    public static (byte[] RawBytes, string HexPayload) EncodeEddystoneUrl(string url, sbyte txPower = -20)
    {
        byte urlPrefixCode = 0x03; // https://
        string remainder = url;

        if (url.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase))
        {
            urlPrefixCode = 0x01;
            remainder = url[12..];
        }
        else if (url.StartsWith("http://www.", StringComparison.OrdinalIgnoreCase))
        {
            urlPrefixCode = 0x00;
            remainder = url[11..];
        }
        else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            urlPrefixCode = 0x03;
            remainder = url[8..];
        }
        else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            urlPrefixCode = 0x02;
            remainder = url[7..];
        }

        byte[] urlEncoded = Encoding.UTF8.GetBytes(remainder);
        byte[] payload = new byte[10 + urlEncoded.Length];

        payload[0] = 0x02; // Flags length
        payload[1] = 0x01; // Flags type
        payload[2] = 0x06; // Flags value

        payload[3] = (byte)(6 + urlEncoded.Length); // Service data length (UUID 2B + FrameType 1B + TxPower 1B + Prefix 1B + URL)
        payload[4] = 0x16; // Service Data 16-bit UUID
        payload[5] = 0xAA; // Eddystone 16-bit UUID Low
        payload[6] = 0xFE; // Eddystone 16-bit UUID High
        payload[7] = 0x10; // Frame type: URL
        payload[8] = (byte)txPower;
        payload[9] = urlPrefixCode;

        Array.Copy(urlEncoded, 0, payload, 10, urlEncoded.Length);

        string hex = SpanExtensions.ToHexStringFast(payload);
        return (payload, hex);
    }
}

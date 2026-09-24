using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Generators;
using OpenScanSim.Core.Protocols;
using Xunit;

namespace OpenScanSim.UnitTests;

public class AdditionalCoreTests
{
    [Fact]
    public void BarcodeSymbologyEncoder_Ean13Formatting_AppendsValidCheckDigit()
    {
        // Arrange: 12-digit base EAN
        string input12 = "400638133393";

        // Act
        string formatted = BarcodeSymbologyEncoder.FormatBarcode(BarcodeSymbology.Ean13, input12, includeCheckDigit: true);

        // Assert
        formatted.Should().Be("4006381333931");
    }

    [Fact]
    public void BarcodeSymbologyEncoder_WithAimPrefix_PrependsCorrectIdentifier()
    {
        // Arrange
        string payload = "12345678";

        // Act
        string formatted = BarcodeSymbologyEncoder.FormatBarcode(
            BarcodeSymbology.Code128,
            payload,
            includeCheckDigit: false,
            includeAimPrefix: true);

        // Assert
        formatted.Should().Be("]C112345678");
    }

    [Fact]
    public void ScanRecord_ToWedgeOutput_FormatsDelimitersCorrectly()
    {
        // Arrange
        ScanRecord record = new(
            ReaderType: ReaderType.UhfRfid,
            PrimaryPayload: "3034257BF400B7800004CB21",
            Tid: "E28011302000000001",
            UserMemoryHex: "A1B2C3D4");

        // Act
        string output = record.ToWedgeOutput(includeTid: true, includeUser: true, delimiter: "|");

        // Assert
        output.Should().Be("3034257BF400B7800004CB21|E28011302000000001|A1B2C3D4");
    }

    [Fact]
    public async Task MmfFileStreamGenerator_ReadsLinesLazily()
    {
        // Arrange: Create temp file with 5 lines
        string tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllLinesAsync(tempFile, ["30340001", "30340002", "# comment", "30340003"]);

            MmfFileStreamGenerator generator = new();
            GeneratorConfig config = new()
            {
                ReaderType = ReaderType.UhfRfid,
                SourceFilePath = tempFile,
                TotalCount = 100
            };

            // Act
            List<ScanRecord> records = [];
            await foreach (var item in generator.GenerateStreamAsync(config))
            {
                records.Add(item);
            }

            // Assert
            records.Should().HaveCount(3);
            records[0].PrimaryPayload.Should().Be("30340001");
            records[1].PrimaryPayload.Should().Be("30340002");
            records[2].PrimaryPayload.Should().Be("30340003");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}

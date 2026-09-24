using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OpenScanSim.Cli;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Generators;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Validators;
using Xunit;

namespace OpenScanSim.UnitTests;

public sealed class Sprint6EndToEndAndEdgeCaseTests
{
    [Fact]
    public void BarcodeSymbologyEncoder_QR_And_DataMatrix_ShouldFormatCorrectly()
    {
        var qrData = "https://rfidsoftwares.com/open-scan-sim";
        var qrEncoded = BarcodeSymbologyEncoder.FormatBarcode(BarcodeSymbology.QrCode, qrData, includeCheckDigit: false, includeAimPrefix: true);
        qrEncoded.Should().Be("]Q1" + qrData);

        var dmData = "010890123456789021SERIAL123";
        var dmEncoded = BarcodeSymbologyEncoder.FormatBarcode(BarcodeSymbology.DataMatrix, dmData, includeCheckDigit: false, includeAimPrefix: true);
        dmEncoded.Should().Be("]d2" + dmData);
    }

    [Fact]
    public void NdefMessageEncoder_TextUtf8_ShouldIncludeCorrectStatusAndLanguage()
    {
        var payload = NdefMessageEncoder.CreateTextRecord("RFID Softwares India", "en");
        payload.Should().NotBeEmpty();
        var hex = NdefMessageEncoder.ToHexPayload(payload);
        hex.Should().NotBeNullOrWhiteSpace();

        // Header verification
        payload[0].Should().Be(0xD1); // TNF Well-Known
        payload[1].Should().Be(0x01); // Type Length ('T')
        payload[3].Should().Be((byte)'T');
        payload[4].Should().Be(2);    // Status byte (len("en") = 2)
    }

    [Fact]
    public async Task MmfFileStreamGenerator_ShouldStreamCsvRecordsLazily()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("3034257BF400B7800004D2");
            sb.AppendLine("3034257BF400B7800004D3");
            sb.AppendLine("3034257BF400B7800004D4");
            await File.WriteAllTextAsync(tempFile, sb.ToString());

            var config = new GeneratorConfig
            {
                ReaderType = ReaderType.UhfRfid,
                SourceFilePath = tempFile,
                TotalCount = 3
            };

            var generator = new MmfFileStreamGenerator();
            var records = new List<ScanRecord>();
            await foreach (var rec in generator.GenerateStreamAsync(config, CancellationToken.None))
            {
                records.Add(rec);
            }

            records.Should().HaveCount(3);
            records[0].PrimaryPayload.Should().Be("3034257BF400B7800004D2");
            records[1].PrimaryPayload.Should().Be("3034257BF400B7800004D3");
            records[2].PrimaryPayload.Should().Be("3034257BF400B7800004D4");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ProtocolValidator_ShouldRejectInvalidPayloads()
    {
        var validator = new ProtocolValidator();

        var invalidEan = "12345";
        var eanResult = validator.Validate(ReaderType.Barcode1D2D, invalidEan, BarcodeSymbology.Ean13);
        eanResult.IsValid.Should().BeFalse();

        var validEpc = "3034257BF400B7800004D200";
        var epcResult = validator.Validate(ReaderType.UhfRfid, validEpc);
        epcResult.IsValid.Should().BeTrue();

        var invalidEpc = "XYZ-NOT-HEX";
        var epcInvalidResult = validator.Validate(ReaderType.UhfRfid, invalidEpc);
        epcInvalidResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CliRunner_ShouldHandleHelpAndInvalidOptionsGracefully()
    {
        var helpResult = await Program.Main(["help"]);
        helpResult.Should().Be(0);

        var invalidResult = await Program.Main(["unknown-command"]);
        invalidResult.Should().NotBe(0);
    }
}

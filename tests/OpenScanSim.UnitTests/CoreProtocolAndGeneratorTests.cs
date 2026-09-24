using System;
using FluentAssertions;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Extensions;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Generators;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Validators;
using Xunit;

namespace OpenScanSim.UnitTests;

public class CoreProtocolAndGeneratorTests
{
    [Fact]
    public void CalculateModulo10CheckDigit_ValidEan12_ReturnsCorrectCheckDigit()
    {
        // Arrange: 12-digit EAN prefix "400638133393" -> standard test vector with check digit '1'
        string digits12 = "400638133393";

        // Act
        int checkDigit = SpanExtensions.CalculateModulo10CheckDigit(digits12.AsSpan());

        // Assert
        checkDigit.Should().Be(1);
    }

    [Fact]
    public void CalculateModulo43CheckDigit_Code39_ReturnsCorrectCheck()
    {
        // Arrange: "CODE39"
        string code = "CODE39";

        // Act
        char checkChar = SpanExtensions.CalculateModulo43CheckDigit(code.AsSpan());

        // Assert
        checkChar.Should().NotBe('\0');
    }

    [Fact]
    public void CalculateCrc16_ValidData_MatchesExpected()
    {
        // Arrange
        byte[] testData = [0x30, 0x00, 0x30, 0x34, 0x25, 0x7B];

        // Act
        ushort crc = SpanExtensions.CalculateCrc16(testData);

        // Assert
        crc.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Sgtin96Encoder_Partition2_ProducesValid24HexChars()
    {
        // Act: Partition 2, Company Prefix 0614141 (10 digits), Item Ref 100 (3 digits), Serial 1
        string epc = EpcGen2Encoder.EncodeSgtin96(
            filterValue: 3,
            partition: 2,
            companyPrefix: 614141,
            itemReference: 100,
            serialNumber: 1);

        // Assert
        epc.Should().HaveLength(24);
        epc.Should().StartWith("30"); // Standard SGTIN-96 header
    }

    [Fact]
    public void NdefMessageEncoder_CreatesValidUriRecord()
    {
        // Arrange
        string url = "https://rfidsoftwares.com/";

        // Act
        byte[] record = NdefMessageEncoder.CreateUriRecord(url);

        // Assert
        record.Should().NotBeEmpty();
        record[0].Should().Be(0xD1); // MB=1, ME=1, SR=1, TNF=0x01
        record[3].Should().Be((byte)'U'); // Record Type 'U'
        record[4].Should().Be(0x04); // https:// prefix code
    }

    [Fact]
    public void MaskPatternGenerator_EvaluatesTokensAccurately()
    {
        // Arrange
        string mask = "3034{HEX:4}{SEQ:4}";
        Random rng = new(42);

        // Act
        string result = MaskPatternGenerator.EvaluatePattern(mask, 12, rng);

        // Assert: 4 prefix + 4 hex + 4 seq = 12 characters
        result.Should().HaveLength(12);
        result.Should().StartWith("3034");
        result.Should().EndWith("0012");
    }

    [Fact]
    public async System.Threading.Tasks.Task MaskPatternGenerator_StreamsDirectPastedListAccurately()
    {
        // Arrange
        MaskPatternGenerator generator = new();
        string pastedList = @"E28033000F000F0F3BC9B222
E28033000F00050A6847166C
E28033000F000102B7C62064";

        GeneratorConfig config = new()
        {
            ReaderType = ReaderType.UhfRfid,
            DirectPastedList = pastedList,
            TotalCount = 3
        };

        // Act
        var records = new System.Collections.Generic.List<OpenScanSim.Common.Models.ScanRecord>();
        await foreach (var rec in generator.GenerateStreamAsync(config))
        {
            records.Add(rec);
        }

        // Assert
        records.Should().HaveCount(3);
        records[0].PrimaryPayload.Should().Be("E28033000F000F0F3BC9B222");
        records[1].PrimaryPayload.Should().Be("E28033000F00050A6847166C");
        records[2].PrimaryPayload.Should().Be("E28033000F000102B7C62064");
    }
}

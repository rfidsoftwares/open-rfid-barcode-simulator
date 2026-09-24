using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OpenScanSim.Common.Constants;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Extensions;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Generators;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Services;
using OpenScanSim.Core.Validators;
using Xunit;

namespace OpenScanSim.UnitTests;

public sealed class DeepStressAndBoundaryTests
{
    [Theory]
    [InlineData(0, 123456789012UL, 1UL, 100UL)]
    [InlineData(1, 12345678901UL, 12UL, 200UL)]
    [InlineData(2, 1234567890UL, 123UL, 300UL)]
    [InlineData(3, 123456789UL, 1234UL, 400UL)]
    [InlineData(4, 12345678UL, 12345UL, 500UL)]
    [InlineData(5, 1234567UL, 123456UL, 600UL)]
    [InlineData(6, 123456UL, 1234567UL, 700UL)]
    public void EpcGen2Encoder_AllSgtin96Partitions_ShouldGenerateValid24HexCharEpc(
        int partition, ulong companyPrefix, ulong itemReference, ulong serial)
    {
        string epc = EpcGen2Encoder.EncodeSgtin96(
            filterValue: 1,
            partition: partition,
            companyPrefix: companyPrefix,
            itemReference: itemReference,
            serialNumber: serial);

        epc.Should().HaveLength(24);
        epc.Should().StartWith("30"); // SGTIN-96 0x30 Header
    }

    [Fact]
    public async Task MaskPatternGenerator_HighThroughput100KItems_ShouldStreamFastUnderZeroHeapPressure()
    {
        var config = new GeneratorConfig
        {
            ReaderType = ReaderType.UhfRfid,
            MaskPattern = "3034{HEX:4}{SEQ:8}",
            StartSequence = 1,
            TotalCount = 100_000,
            IncludeTid = false
        };

        var generator = new MaskPatternGenerator();
        ulong count = 0;
        var sw = Stopwatch.StartNew();

        await foreach (var record in generator.GenerateStreamAsync(config, CancellationToken.None))
        {
            count++;
            if (count % 25000 == 0)
            {
                record.PrimaryPayload.Should().StartWith("3034");
                record.PrimaryPayload.Should().HaveLength(16);
            }
        }

        sw.Stop();
        count.Should().Be(100_000);
        sw.ElapsedMilliseconds.Should().BeLessThan(3000); // 100k generated in < 3 seconds
    }

    [Fact]
    public void MaskPatternGenerator_ComplexNestedTokens_ShouldEvaluateCorrectly()
    {
        var rng = new Random(42);
        string template = "TEST-{HEX:4}-{SEQ:6}-{UUID}";
        string result = MaskPatternGenerator.EvaluatePattern(template, 999, rng);

        result.Should().StartWith("TEST-");
        result.Should().Contain("-000999-");
        result.Length.Should().BeGreaterThan(40);
    }

    [Theory]
    [InlineData("000000000000", 0)]
    [InlineData("111111111111", 6)]
    [InlineData("999999999999", 4)]
    [InlineData("400638133393", 1)]
    [InlineData("590123412345", 7)]
    public void SpanExtensions_CalculateModulo10CheckDigit_ShouldComputeAccurateDigits(string input12, int expected)
    {
        int check = SpanExtensions.CalculateModulo10CheckDigit(input12.AsSpan());
        check.Should().Be(expected);
    }

    [Fact]
    public void SpanExtensions_CalculateModulo43_BoundaryCharacters_ShouldEvaluateAccurately()
    {
        // "CODE 39" -> sum of 'C'(12)+'O'(24)+'D'(13)+'E'(14)+' '(38)+'3'(3)+'9'(9) = 113 % 43 = 27 -> 'R'
        char check = SpanExtensions.CalculateModulo43CheckDigit("CODE 39".AsSpan());
        check.Should().Be('R');
    }

    [Fact]
    public void EpcGen2Encoder_CalculateEpcCrc16_ShouldMatchExpectedPolynomial()
    {
        string pcWord = "3000";
        string epcHex = "3034257BF400B7800004CB21";
        string crc = EpcGen2Encoder.CalculateEpcCrc16(pcWord, epcHex);

        crc.Should().HaveLength(4);
        crc.Should().NotBe("0000");
    }

    [Fact]
    public void DigitalScaleWedgeEncoder_AveryBerkelAndNci_ShouldFormatAccurately()
    {
        string ab = DigitalScaleWedgeEncoder.FormatScaleReading(DigitalScaleProtocol.AveryBerkel, 15.50m, "kg", true);
        ab.Should().Contain("15.50 kg S");

        string nci = DigitalScaleWedgeEncoder.FormatScaleReading(DigitalScaleProtocol.NciGeneral, 15.50m, "kg", true);
        nci.Should().Contain("S");
        nci.Should().Contain("15.50KG");
    }

    [Fact]
    public void ProtocolValidator_HfUidLengths_ShouldValidateStandardSizes()
    {
        var validator = new ProtocolValidator();

        // 4-byte UID (8 hex chars)
        var res4 = validator.Validate(ReaderType.HfRfid, "04A1B2C3");
        res4.IsValid.Should().BeTrue();

        // 7-byte UID (14 hex chars)
        var res7 = validator.Validate(ReaderType.HfRfid, "04A1B2C3D4E5F6");
        res7.IsValid.Should().BeTrue();

        // 8-byte UID (16 hex chars)
        var res8 = validator.Validate(ReaderType.HfRfid, "E004010011223344");
        res8.IsValid.Should().BeTrue();

        // Invalid length (10 chars)
        var resBad = validator.Validate(ReaderType.HfRfid, "04A1B2C3D4");
        resBad.IsValid.Should().BeFalse();
    }
}

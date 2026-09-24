using System;
using FluentAssertions;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Extensions;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Services;
using OpenScanSim.Platform.Windows;
using Xunit;

namespace OpenScanSim.UnitTests;

public sealed class ExtendedStudioAndSensorsTests
{
    [Fact]
    public void IcaoPassportMrzEncoder_ShouldCalculateValidTd3Passport()
    {
        var dob = new DateTime(1995, 8, 15);
        var expiry = new DateTime(2035, 8, 14);

        var (line1, line2, combined) = IcaoPassportMrzEncoder.EncodeTd3Passport(
            issuingCountry: "IND",
            surname: "SHARMA",
            givenNames: "AKASH",
            documentNumber: "Z9876543",
            nationality: "IND",
            dateOfBirth: dob,
            sex: 'M',
            dateOfExpiry: expiry,
            personalNumber: "A123456789");

        line1.Should().HaveLength(44);
        line2.Should().HaveLength(44);
        line1.Should().StartWith("P<INDSHARMA<<AKASH");
        line2.Should().StartWith("Z9876543");
        line2.Should().Contain("IND950815");
        line2.Should().Contain("M350814");
    }

    [Fact]
    public void SpanExtensions_IcaoCheckDigit_ShouldMatchKnownStandardVectors()
    {
        // Vector: "L898902C3" -> 6
        int check1 = SpanExtensions.CalculateIcaoCheckDigit("L898902C3".AsSpan());
        check1.Should().Be(6);

        // Vector: "740812" -> 2
        int check2 = SpanExtensions.CalculateIcaoCheckDigit("740812".AsSpan());
        check2.Should().Be(2);

        // Vector: "120415" -> 9
        int check3 = SpanExtensions.CalculateIcaoCheckDigit("120415".AsSpan());
        check3.Should().Be(9);
    }

    [Fact]
    public void DigitalScaleWedgeEncoder_MettlerToledo_ShouldFormatStxAndPayload()
    {
        string output = DigitalScaleWedgeEncoder.FormatScaleReading(
            DigitalScaleProtocol.MettlerToledoContinuous,
            weight: 24.50m,
            unit: "kg",
            isStable: true,
            isGross: true,
            tareWeight: 0.00m);

        output.Should().StartWith("\x02");
        output.Should().EndWith("\r");
        output.Should().Contain("0024.50");
    }

    [Fact]
    public void DigitalScaleWedgeEncoder_CasStandard_ShouldFormatCrLfAndUnits()
    {
        string output = DigitalScaleWedgeEncoder.FormatScaleReading(
            DigitalScaleProtocol.CasStandard,
            weight: 12.345m,
            unit: "kg",
            isStable: true,
            isGross: true);

        output.Should().Be("ST,GS,+012.345 KG\r\n");
    }

    [Fact]
    public void BleBeaconWedgeEncoder_IBeacon_ShouldGenerateStandard30BytePdu()
    {
        var uuid = Guid.Parse("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0");
        var (payload, hex) = BleBeaconWedgeEncoder.EncodeIBeacon(uuid, major: 100, minor: 1, measuredPowerRssiAt1M: -59);

        payload.Should().HaveCount(30);
        hex.Should().HaveLength(60);
        payload[0].Should().Be(0x02); // Length 2
        payload[1].Should().Be(0x01); // Flags type
        payload[4].Should().Be(0xFF); // Manufacturer specific
        payload[5].Should().Be(0x4C); // Apple ID Low
        payload[6].Should().Be(0x00); // Apple ID High
        payload[7].Should().Be(0x02); // iBeacon subtype
        payload[8].Should().Be(0x15); // Length 21
    }

    [Fact]
    public void BleBeaconWedgeEncoder_EddystoneUrl_ShouldEncodeCorrectServiceData()
    {
        var (payload, hex) = BleBeaconWedgeEncoder.EncodeEddystoneUrl("https://rfidsoftwares.com");

        payload.Should().NotBeEmpty();
        hex.Should().NotBeEmpty();
        payload[4].Should().Be(0x16); // Service Data
        payload[5].Should().Be(0xAA); // Eddystone 16-bit UUID Low
        payload[6].Should().Be(0xFE); // Eddystone 16-bit UUID High
        payload[7].Should().Be(0x10); // Frame type: URL
    }

    [Fact]
    public void ChaosNoiseInjector_ShouldInjectConfiguredFaults()
    {
        var injector = new ChaosNoiseInjector();
        var original = new ScanRecord(ReaderType.UhfRfid, "3034257BF400B7800004D2");

        // 100% rate NoRead
        var noRead = injector.ApplyChaos(original, 100.0, ChaosFaultMode.NoReadString);
        noRead.PrimaryPayload.Should().Be("NOREAD");
        noRead.IsFault.Should().BeTrue();

        // 100% rate Truncate
        var truncated = injector.ApplyChaos(original, 100.0, ChaosFaultMode.TruncatePayload);
        truncated.PrimaryPayload.Length.Should().BeLessThan(original.PrimaryPayload.Length);
        truncated.IsFault.Should().BeTrue();

        // 0% rate should not modify record
        var unmodified = injector.ApplyChaos(original, 0.0, ChaosFaultMode.NoReadString);
        unmodified.PrimaryPayload.Should().Be("3034257BF400B7800004D2");
        unmodified.IsFault.Should().BeFalse();
    }

    [Fact]
    public void TargetProcessTracker_IsTargetWindowFocused_WithNullTarget_ReturnsTrue()
    {
        var tracker = new TargetProcessTracker();
        bool isFocused = tracker.IsTargetWindowFocused(null);
        isFocused.Should().BeTrue();
    }
}

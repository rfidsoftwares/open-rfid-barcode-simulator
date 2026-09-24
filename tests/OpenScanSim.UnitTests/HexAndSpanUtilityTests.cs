using System;
using FluentAssertions;
using OpenScanSim.Common.Extensions;
using Xunit;

namespace OpenScanSim.UnitTests;

public class HexAndSpanUtilityTests
{
    [Fact]
    public void TryDecodeHex_ValidHex_DecodesCorrectBytes()
    {
        // Arrange
        string hex = "3034257BF400B7800004CB21";
        Span<byte> buffer = stackalloc byte[12];

        // Act
        bool success = SpanExtensions.TryDecodeHex(hex.AsSpan(), buffer, out int bytesWritten);

        // Assert
        success.Should().BeTrue();
        bytesWritten.Should().Be(12);
        buffer[0].Should().Be(0x30);
        buffer[1].Should().Be(0x34);
        buffer[2].Should().Be(0x25);
    }

    [Fact]
    public void TryDecodeHex_OddLength_ReturnsFalse()
    {
        // Arrange
        string hex = "303";
        Span<byte> buffer = stackalloc byte[2];

        // Act
        bool success = SpanExtensions.TryDecodeHex(hex.AsSpan(), buffer, out int bytesWritten);

        // Assert
        success.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Theory]
    [InlineData("303G")]
    [InlineData("30:4")]
    [InlineData("30@4")]
    [InlineData("ZZZZ")]
    public void TryDecodeHex_InvalidNonHexCharacters_ReturnsFalse(string invalidHex)
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[invalidHex.Length / 2];

        // Act
        bool success = SpanExtensions.TryDecodeHex(invalidHex.AsSpan(), buffer, out int bytesWritten);

        // Assert
        success.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void ToHexStringFast_ProducesExactUppercaseString()
    {
        // Arrange
        byte[] bytes = [0x30, 0xA1, 0xFF, 0x00];

        // Act
        string hex = SpanExtensions.ToHexStringFast(bytes);

        // Assert
        hex.Should().Be("30A1FF00");
    }
}

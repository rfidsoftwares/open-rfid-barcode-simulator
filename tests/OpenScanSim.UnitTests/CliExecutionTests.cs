using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using OpenScanSim.Cli;
using Xunit;

namespace OpenScanSim.UnitTests;

public class CliExecutionTests
{
    [Fact]
    public async Task Cli_HelpCommand_ReturnsZeroExitCode()
    {
        // Act
        int exitCode = await Program.Main(["help"]);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Cli_VersionCommand_ReturnsZeroExitCode()
    {
        // Act
        int exitCode = await Program.Main(["version"]);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Cli_ValidateValidEan13_ReturnsZero()
    {
        // Act
        int exitCode = await Program.Main(["validate", "--type", "barcode", "--symbology", "ean13", "--data", "4006381333931"]);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Cli_ValidateInvalidEan13_ReturnsNonZero()
    {
        // Act (invalid check digit: 9 instead of 1)
        int exitCode = await Program.Main(["validate", "--type", "barcode", "--symbology", "ean13", "--data", "4006381333939"]);

        // Assert
        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task Cli_GenerateToFile_WritesExpectedCsvLines()
    {
        // Arrange
        string tempCsv = Path.GetTempFileName();

        try
        {
            // Act
            int exitCode = await Program.Main(["generate", "--mask", "3034{HEX:4}{SEQ:4}", "--count", "5", "--start", "1", "--output", tempCsv]);

            // Assert
            exitCode.Should().Be(0);
            File.Exists(tempCsv).Should().BeTrue();
            string[] lines = await File.ReadAllLinesAsync(tempCsv);
            lines.Length.Should().Be(6); // 1 header + 5 rows
            lines[0].Should().Be("Sequence,Payload");
            lines[1].Should().StartWith("1,3034");
            lines[5].Should().StartWith("5,3034");
        }
        finally
        {
            if (File.Exists(tempCsv)) File.Delete(tempCsv);
        }
    }
}

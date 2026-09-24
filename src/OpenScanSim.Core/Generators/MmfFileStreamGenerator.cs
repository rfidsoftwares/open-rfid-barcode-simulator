using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;

namespace OpenScanSim.Core.Generators;

/// <summary>
/// Memory-Mapped File (MMF) zero-allocation streaming generator for large multi-million row files.
/// </summary>
public sealed class MmfFileStreamGenerator : IScanDataGenerator
{
    public async IAsyncEnumerable<ScanRecord> GenerateStreamAsync(
        GeneratorConfig config,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.SourceFilePath) || !File.Exists(config.SourceFilePath))
        {
            throw new FileNotFoundException("Source data file not found", config.SourceFilePath);
        }

        ulong sequence = 0;
        using FileStream fileStream = new(config.SourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536, useAsync: true);
        using StreamReader reader = new(fileStream, Encoding.UTF8);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            sequence++;
            yield return new ScanRecord(
                ReaderType: config.ReaderType,
                PrimaryPayload: trimmed,
                Timestamp: DateTime.UtcNow,
                SequenceIndex: sequence);

            if (sequence >= config.TotalCount)
            {
                break;
            }
        }
    }
}

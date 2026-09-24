using System.Collections.Generic;
using System.Threading;
using OpenScanSim.Common.Models;

namespace OpenScanSim.Core.Generators;

/// <summary>
/// Abstraction for all streaming scan data generators.
/// </summary>
public interface IScanDataGenerator
{
    /// <summary>
    /// Lazily streams generated scan records on-the-fly with minimal memory footprint.
    /// </summary>
    IAsyncEnumerable<ScanRecord> GenerateStreamAsync(
        GeneratorConfig config,
        CancellationToken cancellationToken = default);
}

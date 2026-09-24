namespace OpenScanSim.Common.Enums;

/// <summary>
/// Supported auto-identification hardware reader types.
/// </summary>
public enum ReaderType
{
    Barcode1D2D,
    HfRfid,
    Nfc,
    UhfRfid,
    OcrPassport,
    ScaleWedge,
    BleBeacon
}

/// <summary>
/// Keystroke output simulation mode.
/// </summary>
public enum SimulationMode
{
    /// <summary>
    /// Atomic batch injection mimicking hardware USB HID scanner (&lt;10ms burst).
    /// </summary>
    FastDeviceBurst,

    /// <summary>
    /// Character-by-character typing with Gaussian jitter and optional typo correction.
    /// </summary>
    HumanTyping
}

/// <summary>
/// Output line terminator sent after payload.
/// </summary>
public enum OutputTerminator
{
    Enter,
    Tab,
    Space,
    None,
    Custom
}

/// <summary>
/// 1D and 2D barcode symbologies.
/// </summary>
public enum BarcodeSymbology
{
    Code128,
    Code39,
    Code93,
    Ean13,
    Ean8,
    UpcA,
    UpcE,
    Itf14,
    Codabar,
    QrCode,
    DataMatrix,
    Pdf417,
    Aztec
}

/// <summary>
/// Byte endianness for HF/NFC UIDs.
/// </summary>
public enum UidByteOrder
{
    MsbBigEndian,
    LsbLittleEndian
}

/// <summary>
/// Lifecycle state of the simulation engine.
/// </summary>
public enum SimulationState
{
    Idle,
    StartingCountdown,
    Running,
    Paused,
    Completed,
    Faulted
}

/// <summary>
/// Digital scale serial/HID output stream protocol formats.
/// </summary>
public enum DigitalScaleProtocol
{
    MettlerToledoContinuous,
    CasStandard,
    AveryBerkel,
    NciGeneral
}

/// <summary>
/// Hardware chaos and noise injection fault mode.
/// </summary>
public enum ChaosFaultMode
{
    None,
    NoReadString,
    TruncatePayload,
    CorruptCrc,
    DuplicateBurstScan,
    InterleavedDelayNoise
}

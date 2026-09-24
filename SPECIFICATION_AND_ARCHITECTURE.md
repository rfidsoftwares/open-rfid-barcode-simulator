# 📡 OpenRFID & Barcode Simulator
### Universal Virtual HID Keyboard Wedge & Hardware Protocol Simulator
**An Open-Source Project by [RFID Softwares](https://rfidsoftwares.com/) (RFID Software India Private Limited)**  
**Built with C# (.NET 9 LTS) + Avalonia UI (Cross-Platform Architecture)**

---

## 📑 Complete Table of Contents
1. [Executive Summary & Problem Statement](#1-executive-summary--problem-statement)
2. [Target Architecture & Tech Stack (C# + Avalonia UI)](#2-target-architecture--tech-stack-c--avalonia-ui)
3. [Supported Readers & Protocol Data Models](#3-supported-readers--protocol-data-models)
   - [3.1 1D/2D Optical Barcode Scanners & GS1 Standard](#31-1d2d-optical-barcode-scanners--gs1-standard)
   - [3.2 HF RFID (13.56 MHz - ISO 15693 / ISO 14443A)](#32-hf-rfid-1356-mhz---iso-15693--iso-14443a)
   - [3.3 NFC Readers (ISO/IEC 18092 / NFC Forum Type 1–5 / NDEF)](#33-nfc-readers-isoiec-18092--nfc-forum-type-15--ndef)
   - [3.4 UHF RAIN RFID (ISO/IEC 18000-6C / EPC Gen2 / 860–960 MHz)](#34-uhf-rain-rfid-isoiec-18000-6c--epc-gen2--860960-mhz)
   - [3.5 Extended Readers (BLE Beacons, ICAO Passport MRZ, Digital Scales)](#35-extended-readers)
4. [Memory-Efficient Generator & Streaming Architecture](#4-memory-efficient-generator--streaming-architecture)
   - [4.1 Zero-RAM Streaming with `IAsyncEnumerable` & `Span<char>`](#41-zero-ram-streaming-with-iasyncenumerable--spanchar)
   - [4.2 Template Mask & Pattern Parsing Engine](#42-template-mask--pattern-parsing-engine)
   - [4.3 Memory-Mapped File (MMF) Buffered Disk Streamer (50M+ Rows)](#43-memory-mapped-file-mmf-buffered-disk-streamer-50m-rows)
   - [4.4 Mathematical Validation & Checksum Engine](#44-mathematical-validation--checksum-engine)
5. [Keystroke Simulation & Timing Engine](#5-keystroke-simulation--timing-engine)
   - [5.1 Fast Hardware Device Wedge Mode (`SendInput`)](#51-fast-hardware-device-wedge-mode-sendinput)
   - [5.2 Human-Typing Simulation Mode (Gaussian Jitter & Typo Correction)](#52-human-typing-simulation-mode)
   - [5.3 Timing Scheduler, Countdowns, and Multimedia High-Resolution Clocks](#53-timing-scheduler-countdowns-and-multimedia-high-resolution-clocks)
   - [5.4 Prefixes, Suffixes, Delimiters, FNC1 & Hardware Scancodes](#54-prefixes-suffixes-delimiters-fnc1--hardware-scancodes)
6. [Fault Simulation & Chaos Injection Engine](#6-fault-simulation--chaos-injection-engine)
   - [6.2 Tag Memory Bank Lock & CRC Failure Simulation](#62-tag-memory-bank-lock--crc-failure-simulation)
7. [Window Modes, Widget System & Tray Integration](#7-window-modes-widget-system--tray-integration)
   - [7.1 The 3-Tier Window State Architecture](#71-the-3-tier-window-state-architecture)
   - [7.2 Always-on-Top & Focus Non-Stealing (`WS_EX_NOACTIVATE`)](#72-always-on-top--focus-non-stealing-ws_ex_noactivate)
   - [7.3 Compact Floating Mini-HUD Widget & Screen Snapping](#73-compact-floating-mini-hud-widget--screen-snapping)
   - [7.4 Background System Tray Daemon with Quick-Action Menu](#74-background-system-tray-daemon-with-quick-action-menu)
8. [Critical Edge Cases & Real-World Hardware Nuances](#8-critical-edge-cases--real-world-hardware-nuances)
   - [8.1 Windows UIPI (Elevated Admin App Boundaries)](#81-windows-uipi-elevated-admin-app-boundaries)
   - [8.2 CapsLock, Active Modifiers & IME Swallow Immunity](#82-capslock-active-modifiers--ime-swallow-immunity)
   - [8.3 Windows Timer Resolution & `timeBeginPeriod(1)`](#83-windows-timer-resolution--timebeginperiod1)
   - [8.4 UHF Multi-Tag Collisions & Session Flag Decay](#84-uhf-multi-tag-collisions--session-flag-decay)
   - [8.5 Target Window Locking (`HWND`) vs Active Focus](#85-target-window-locking-hwnd-vs-active-focus)
9. [Cross-Platform Native Input Abstraction (Windows / Linux / macOS)](#9-cross-platform-native-input-abstraction)
10. [UI/UX Design, Visual Mockups & Component Tour](#10-uiux-design-visual-mockups--component-tour)
11. [Codebase Standards & Clean Architecture Conventions](#11-codebase-standards--clean-architecture-conventions)
    - [11.1 Structural Layering (Core, Platform, UI, Common)](#111-structural-layering-core-platform-ui-common)
    - [11.2 Service, Config, Model, Util & Constant Conventions](#112-service-config-model-util--constant-conventions)
    - [11.3 Memory Optimization & Zero-Allocation Rules](#113-memory-optimization--zero-allocation-rules)
12. [Quality Control (QC), 4-Tier Testing & AI Verification](#12-quality-control-qc-4-tier-testing--ai-verification)
    - [12.1 Tier 1: Unit Testing (xUnit + FluentAssertions)](#121-tier-1-unit-testing-xunit--fluentassertions)
    - [12.2 Tier 2: Integration & Stream Throughput Testing](#122-tier-2-integration--stream-throughput-testing)
    - [12.3 Tier 3: System & Hardware Wedge Testing](#123-tier-3-system--hardware-wedge-testing)
    - [12.4 Tier 4: Autonomous AI Real-World Browser Verification](#124-tier-4-autonomous-ai-real-world-browser-verification)
    - [12.5 Automated Testing Report Generation](#125-automated-testing-report-generation)
13. [C# Solution Architecture & Class Implementation Details](#13-c-solution-architecture--class-implementation-details)
14. [Open-Source Roadmap, Milestones & Contribution](#14-open-source-roadmap-milestones--contribution)
15. [Appendices & Reference Implementations](#15-appendices--reference-implementations)

---

## 1. Executive Summary & Problem Statement

Developers and QA teams building retail POS, warehouse WMS, hospital inventory, library management, and supply-chain logistics applications constantly struggle to test systems without physical barcode scanners, UHF RFID portal antennas, or NFC desktop readers.

Existing testing methods suffer from major limitations:
1. **High Hardware Cost & Logistics**: Setting up 4-antenna UHF RFID portals or procuring 15 different scanner models for development teams is expensive and slow.
2. **Memory Crashes with Large Data**: Most existing software simulators generate lists in RAM, crashing or causing severe Garbage Collection freezes when testing queues of 1,000,000 to 50,000,000 items.
3. **Clipboard Inaccuracy**: Copy-pasting text from the clipboard bypasses genuine keyboard buffer behaviors, failing to test realistic typing speeds, AIM code prefixes, FNC1 group separators, and character scancode triggers.

**OpenScanSim** is a high-performance open-source hardware simulator and virtual HID keyboard wedge built in **C# (.NET 9) + Avalonia UI** that emulates physical hardware scanners and RFID readers with zero memory overhead, sub-millisecond OS input injection, and 100% protocol fidelity.

---

## 2. Target Architecture & Tech Stack (C# + Avalonia UI)

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                               Presentation Layer (Avalonia UI)                         │
│   • Main Dashboard   • Floating Mini-HUD   • Advanced Inspector   • Live Telemetry     │
├────────────────────────────────────────────────────────────────────────────────────────┤
│                          Application & MVVM Layer (CommunityToolkit)                   │
│   • SimulationViewModel   • GeneratorViewModel   • TelemetryViewModel   • RelayCommands│
├────────────────────────────────────────────────────────────────────────────────────────┤
│                               Core Domain & Engine Layer                               │
│   • IScanDataGenerator   • ProtocolEncoders   • Validators   • HighResolutionTimer     │
├────────────────────────────────────────────────────────────────────────────────────────┤
│                              Platform Native Keystroke Drivers                         │
│   • Win32 SendInput (Windows)   • /dev/uinput (Linux)   • Quartz CGEvent (macOS)       │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

| Layer | Component | Implementation | Rationale |
| :--- | :--- | :--- | :--- |
| **Runtime** | .NET 9 LTS | C# 13 | High throughput, `Span<char>`, `IAsyncEnumerable`, native memory performance. |
| **GUI Framework** | Avalonia UI v11+ | Cross-Platform XAML | Skia-based rendering, rich dark-mode themes, native Windows feel with Linux/macOS support. |
| **MVVM Foundation** | CommunityToolkit.Mvvm | Source Generators | Zero reflection, high performance `[ObservableProperty]`, `[RelayCommand]`. |
| **Input Injection** | Native OS P/Invoke | `user32.dll` (`SendInput`) | True kernel-level keyboard events, bypassing layout and IME issues via Unicode scancodes. |
| **Audio Synthesis** | Low-latency PCM Stream | Cross-platform Synth | Configurable physical hardware scanner beep sounds (frequency, duration, volume). |
| **License** | Permissive Open-Source | MIT / Apache 2.0 | Maximum flexibility for commercial and enterprise adoption. |

---

## 3. Supported Readers & Protocol Data Models

### 3.1 1D/2D Optical Barcode Scanners & GS1 Standard
Simulates commercial optical scanners (Zebra DS2208/DS3678, Honeywell Xenon 1900, Datalogic Gryphon):
* **1D Symbologies**:
  * **Code 128 (Subsets A, B, C)**: Alphanumeric and high-density double-digit numeric encoding.
  * **Code 39 & Code 93**: With optional Modulo 43 check character.
  * **EAN-13, EAN-8, UPC-A, UPC-E**: With strict Modulo 10 check digit verification.
  * **Interleaved 2 of 5 (ITF-14)** & **Codabar**.
* **2D Symbologies**:
  * **QR Code & Micro QR**: Standard alphanumeric, binary, and Kanji modes.
  * **Data Matrix (ECC 200)**: Square and rectangular industrial formats.
  * **PDF417 & Aztec Code**: Transport and identification symbologies.
* **GS1 Application Identifiers (AI)**:
  * Full emulation of AI syntax: `(01)GTIN`, `(10)Batch/Lot`, `(17)Expiration Date`, `(21)Serial Number`.
  * Proper injection of the **FNC1 Group Separator** `[GS]` (`ASCII 29` / `0x1D`) when variable-length AI fields are transmitted.
* **AIM Symbology Identifiers**:
  * Option to prepend standard AIM prefixes (e.g. `]C1` for GS1-128, `]Q1` for QR Code, `]d2` for GS1 DataMatrix).

### 3.2 HF RFID (13.56 MHz - ISO 15693 / ISO 14443A)
Simulates proximity/vicinity desktop readers (Feig Electronic, Omnikey, ACR122U):
* **ISO 15693 (Vicinity Cards / Library Tags)**:
  * 64-bit Unique Identifier (UID): e.g. `E004015012345678`.
  * **AFI (Application Family Identifier)**: 1-byte library security code (e.g., `0xC2` for checked-in item, `0x07` for checked-out item).
  * **DSFID (Data Storage Format Identifier)**: 1-byte data format indicator.
  * **User Block Memory**: 28–64 blocks of 4 bytes each (Hex or ASCII).
* **ISO 14443A (Proximity / MIFARE)**:
  * 4-byte (Single size) and 7-byte (Double size) CSN/UIDs.
  * **UID Byte Ordering Selector**: MSB (Big-Endian) vs LSB (Little-Endian / Reverse Byte Order, popular in ACR122U legacy readers).
  * MIFARE Classic 1K/4K Sector Trailer and Data Block dumps.

### 3.3 NFC Readers (ISO/IEC 18092 / NFC Forum Type 1–5 / NDEF)
* **Tag Types**: NTAG213 (144 bytes), NTAG215 (504 bytes), NTAG216 (888 bytes), MIFARE Ultralight.
* **NDEF Record Formats**:
  * **URI Record (`'U'`)**: Supports standard URI Identifier prefixes (`0x01` = `http://www.,` `0x03` = `http://`, `0x04` = `https://`).
  * **Text Record (`'T'`)**: ISO-639-1 language code (e.g., `en`) + UTF-8 / UTF-16 payload with status byte encoding.
  * **MIME Media Record**: `text/vcard` (Contact cards), `application/json` (IoT payloads).
  * **Smart Poster Record (`'Sp'`)**: URI + Multi-language Title + Action code.
  * **Android Application Record (AAR)**: Launches target package name directly on Android devices.

### 3.4 UHF RAIN RFID (ISO/IEC 18000-6C / EPC Gen2 / 860–960 MHz)
Full 4-bank memory structure simulation:
* **Bank 00 (Reserved Memory)**:
  * **Kill Password**: 32-bit hex (permanently disables tag on reader command).
  * **Access Password**: 32-bit hex (required for read/write on locked banks).
* **Bank 01 (EPC Memory)**:
  * **CRC-16**: 16-bit calculated checksum over PC + EPC data.
  * **Protocol Control (PC Word)**: 16-bit header defining EPC length and ISO/IEC 15962 toggle (e.g. `0x3000` for 96-bit EPC).
  * **EPC ID Payload**:
    * 96-bit Hex (24 hex characters) / 128-bit Hex (32 hex characters).
    * GS1 SGTIN-96, SSCC-96, GRAI-96, GIAI-96 with standard partition tables.
    * Pure Identity EPC URI (e.g., `urn:epc:id:sgtin:0614141.100734.123456`).
* **Bank 10 (TID - Tag Identifier)**:
  * Factory-locked 32-bit to 96-bit silicon ID (Impinj Monza 4/5/6/R6, NXP UCODE 8/9, Alien Higgs-4).
* **Bank 11 (User Memory)**:
  * 0 to 512+ bits of customizable customer data.
* **Portal Metadata Emulation**:
  * Prepend/Append RSSI (`-58.4 dBm`), Antenna ID (`Port 2`), Read Count (`12`), Phase Angle (`145.2°`).

### 3.5 Extended Readers
* **BLE Beacons**: iBeacon (UUID, Major, Minor, TxPower, RSSI), Eddystone-UID, Eddystone-TLM.
* **Optical MRZ / Passport Scanner**: ICAO 9303 2-line (TD3) and 3-line (TD1) Machine Readable Zones with check digit verification.
* **Industrial Weighing Scales**: Continuous RS232/USB string wedges (e.g. `ST,GS,+0024.50kg\r\n`).

---

## 4. Memory-Efficient Generator & Streaming Architecture

```
                               ┌─────────────────────────────┐
                               │   Configuration / Mask      │
                               └──────────────┬──────────────┘
                                              ▼
 ┌────────────────────────────────────────────────────────────────────────────────────────┐
 │                      Lazy Infinite Streaming Pipeline (IAsyncEnumerable)               │
 │                                                                                        │
 │  ┌──────────────────────┐      ┌──────────────────────┐      ┌──────────────────────┐  │
 │  │ Sequence Generator   │ ───► │ Mask/Format Resolver │ ───► │ Validator Engine     │  │
 │  │ (Index: 0..10,000,000)│     │ e.g. 3034{HEX:8}{SEQ}│      │ (CRC/Luhn/GS1 Check) │  │
 │  └──────────────────────┘      └──────────────────────┘      └──────────┬───────────┘  │
 └─────────────────────────────────────────────────────────────────────────┼──────────────┘
                                                                           ▼
                                                               ┌───────────────────────┐
                                                               │ Keystroke Output Hook │
                                                               └───────────────────────┘
```

### 4.1 Zero-RAM Streaming with `IAsyncEnumerable` & `Span<char>`
Generating 10,000,000 records in a standard `List<string>` requires ~1.2 GB of heap memory and triggers heavy Garbage Collection pauses. OpenScanSim implements a **Pull-Based Lazy Pipeline**:

```csharp
public interface IScanDataGenerator
{
    IAsyncEnumerable<ScanRecord> GenerateStreamAsync(
        GeneratorConfig config, 
        CancellationToken cancellationToken = default);
}
```

```csharp
// Zero-allocation formatting using string.Create and Span<char>
public sealed class FastSequentialEpcGenerator : IScanDataGenerator
{
    public async IAsyncEnumerable<ScanRecord> GenerateStreamAsync(
        GeneratorConfig config, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ulong currentSeq = config.StartSequence;
        ulong totalCount = config.Count;
        string prefix = config.PrefixHex; // e.g. "30340000"

        for (ulong i = 0; i < totalCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ulong seq = currentSeq + i;
            string epcHex = string.Create(24, (prefix, seq), (span, state) =>
            {
                state.prefix.AsSpan().CopyTo(span);
                state.seq.TryFormat(span.Slice(state.prefix.Length), out _, "X16");
            });

            yield return new ScanRecord
            {
                ReaderType = ReaderType.UhfRfid,
                PrimaryPayload = epcHex,
                ProtocolControlWord = "3000",
                Tid = "E28011302000" + (seq & 0xFFFFFF).ToString("X6"),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
```

* **RAM Footprint**: Strictly **< 30 MB** even when streaming 100,000,000 items continuously.

### 4.2 Template Mask & Pattern Parsing Engine
Users can create dynamic generation templates using a syntax engine:
* `#` = Random Numeric (0-9)
* `X` = Random Alphanumeric (0-9, A-Z)
* `H` = Random Hex character (0-9, A-F)
* `{SEQ:8}` = Sequential counter zero-padded to 8 digits
* `{UUID}` = Standard GUID without hyphens
* `{DATE:yyyyMMdd}` = Current date token
* *Example*: `3034{HEX:6}{DATE:yyMM}{SEQ:8}` $\rightarrow$ `3034A1B2C3260900000042`

### 4.3 Memory-Mapped File (MMF) Buffered Disk Streamer (50M+ Rows)
When importing external CSV/TXT files with millions of pre-existing barcodes/EPCs:
* Reads file via `MemoryMappedFile` and `MemoryExtensions.EnumerateLines`.
* Zero heap allocations for line parsing; lines are sliced directly from the memory-mapped buffer.

### 4.4 Mathematical Validation & Checksum Engine
Every record is validated against strict mathematical rules before transmission:
* **EAN-13 / UPC Check Digit**: Modulo 10 algorithm with alternating weights 1 and 3.
* **GS1 SGTIN-96 Partition Table Check**: Validates Company Prefix bit length vs Item Reference bit length.
* **CRC-16 / CCITT Checksum**: 16-bit polynomial `0x1021` validation on EPC and TID blocks.

---

## 5. Keystroke Simulation & Timing Engine

### 5.1 Fast Hardware Device Wedge Mode (`SendInput`)
Emulates a high-speed physical USB HID barcode/RFID scanner wedge. Keystrokes are submitted to the OS in a single atomic batch:

```csharp
[DllImport("user32.dll", SetLastError = true)]
internal static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    public uint type; // 1 = INPUT_KEYBOARD
    public KEYBDINPUT ki;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}
```

* **Transmission Speed**: A 24-character EPC string + `Enter` suffix is injected in **< 8 milliseconds**.
* **Scan Code Direct Injection**: Uses `KEYEVENTF_SCANCODE` and `KEYEVENTF_UNICODE` for maximum compatibility with virtual desktops (Citrix, RDP, VMware), legacy POS, and browser text fields.

### 5.2 Human-Typing Simulation Mode (Gaussian Jitter & Typo Correction)
Emulates an operator manually entering codes on a physical keyboard:
* **Base Typing Speed**: Configurable average speed (e.g. 110ms per character).
* **Gaussian Jitter Distribution**: Adds random normal variance ($\pm 35\text{ms}$) to each keystroke:
  $$\Delta t = \mu + \sigma \cdot \sqrt{-2 \ln(U_1)} \cos(2\pi U_2)$$
* **Fat-Finger Typo Simulation (Configurable 0%–5%)**:
  * 2% chance of typing an adjacent QWERTY key.
  * Realistic pause (180ms–300ms) representing human realization of mistake.
  * Backspace keystroke (`VK_BACK`).
  * Typing the correct character.

### 5.3 Timing Scheduler, Countdowns, and Multimedia High-Resolution Clocks
1. **Initial Start Delay ($T_{start}$)**: Configurable countdown (e.g. 5.0s, 10.0s). Displays a visual countdown overlay allowing the user to click into their target application.
2. **Queue Interval Delay ($T_{interval}$)**: Delay between successive records (e.g. 1 scan every 2.5 seconds).
3. **Burst/Batch Size**: Transmits $K$ records simultaneously before waiting $T_{interval}$ (simulates multi-tag read bursts in RFID portals).

### 5.4 Prefixes, Suffixes, Delimiters, FNC1 & Hardware Scancodes
* **Configurable Suffix**: `Enter (\r\n)`, `Tab (\t)`, `Space`, `None`, `Custom`.
* **Configurable Prefix**: `STX (0x02)`, `Aim Code ID`, `Custom String`.
* **Bank Separators**: Output multi-bank payloads like `[EPC] | [TID] | [USER] [ENTER]`.

---

## 6. Fault Simulation & Chaos Injection Engine

To help QA teams test edge cases, error handlers, and bad scan recovery in their target software, OpenScanSim includes a configurable **Chaos & Noise Injector**:

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                              CHAOS & FAULT INJECTION MODES                             │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ • NOREAD / Unreadable Barcode (e.g. outputs "NOREAD" or skips suffix)                 │
│ • Partial Scans (e.g. barcode truncated mid-stream to 8 of 12 digits)                  │
│ • Checksum Inversion (intentionally wrong Modulo 10 / CRC-16 check digit)              │
│ • Tag Memory Bank Lock Violation (simulates locked bank access error)                  │
│ • Multi-Tag Collision Backscatter Noise (intermittent tag drops in 500-tag bursts)     │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Window Modes, Widget System & Tray Integration

To provide seamless testing when injecting keystrokes into active web browsers, ERPs, WMS, and POS applications, OpenScanSim adopts a **3-Tier Window State Architecture**:

```
                  ┌───────────────────────────────────────────────────┐
                  │          State 1: Full Config Dashboard           │
                  │  (Setup readers, masks, intervals & profiles)     │
                  └───────────────┬───────────────────▲───────────────┘
                     Click Start  │                   │  Click Expand
                     Simulation   │                   │  Button
                                  ▼                   │
                  ┌───────────────────────────────────┴───────────────┐
                  │    State 2: Always-on-Top Floating Mini-HUD       │
                  │  (Compact 300x160px, translucent, focus-safe)     │
                  └───────────────┬───────────────────▲───────────────┘
                     Click Tray   │                   │  Click Tray
                     Minimize     │                   │  Restore
                                  ▼                   │
                  ┌───────────────────────────────────┴───────────────┐
                  │             State 3: System Tray Daemon           │
                  │  (Hidden near clock, global hotkeys F8/F9/F10)    │
                  └───────────────────────────────────────────────────┘
```

### 7.1 The 3-Tier Window State Architecture
1. **State 1: Full Configuration Dashboard (Primary Window)**:
   * Used for initial setup, mask authoring, protocol selection, and memory bank inspection (~1000×650px).
   * Automatically collapses into State 2 upon starting a simulation to clear the user's screen.
2. **State 2: Compact Floating Mini-HUD (Desktop Widget)**:
   * Extremely compact footprint (`300 × 160 px`), translucent (opacity 85%), pinned to the top layer (`Topmost = true`).
   * Displays only critical in-flight simulation data (countdown wheel, progress bar, active payload, pause/stop buttons).
3. **State 3: Background System Tray Daemon (Minimized / Headless)**:
   * Sits silently in the Windows Taskbar Notification Area (System Tray).
   * Background global hotkey listener remains active 24/7 so users can trigger scans while working in any application without opening the main window.

### 7.2 Always-on-Top & Focus Non-Stealing (`WS_EX_NOACTIVATE`)
* **The Core Usability Problem**: When simulating keyboard input, the user MUST maintain keyboard cursor focus inside the target website input box. If clicking the simulator or if a timer event causes the simulator window to grab OS focus, keystrokes will be typed into the simulator itself rather than the website!
* **The Win32 Solution**:
  * The Floating Mini-HUD window is initialized with the Win32 extended window style `WS_EX_NOACTIVATE` (`0x08000000`) and `WS_EX_TOPMOST` (`0x00000008`):
    ```csharp
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var handle = this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle != IntPtr.Zero && OperatingSystem.IsWindows())
        {
            int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
        }
    }
    ```
  * Users can click `Pause (F9)` or `Stop (F8)` directly on the floating HUD without the target web browser losing its active text cursor focus!

### 7.3 Compact Floating Mini-HUD Widget & Screen Snapping
* **Draggable Anywhere**: Click and drag anywhere on the widget backdrop to reposition.
* **Smart Screen Snapping**: Automatically snaps to screen corners (Top-Right, Bottom-Right, Top-Left, Bottom-Left) with a 12px margin when dragged near screen edges.
* **Opacity Adjustment**: Live slider (50% to 100%) so users can read text on webpages located directly underneath the widget.
* **Position Memory**: Saves X/Y coordinates in local user settings (`settings.json`) across sessions.

### 7.4 Background System Tray Daemon with Quick-Action Menu
* **Tray Icon**: High-DPI animated icon showing current status (Idle 🟢, Countdown 🟡, Transmitting 🔵, Paused ⏸️).
* **Right-Click Context Menu**:
  * `▶ Start Active Queue (F8)`
  * `⚡ Send Single Test Scan (F10)`
  * `🏷️ Quick Preset: Retail EAN-13 Barcode`
  * `🏷️ Quick Preset: UHF SGTIN-96 EPC Tag`
  * `🏷️ Quick Preset: NFC NDEF URL (https://...)`
  * `─────────────────────────────`
  * `🗔 Open Full Dashboard`
  * `📌 Toggle Floating Widget`
  * `❌ Exit OpenScanSim`
* **Windows Toast Notifications**: Dispatches silent non-intrusive notification bubbles upon completing multi-million tag queue executions.

---

## 8. Critical Edge Cases & Real-World Hardware Nuances

### 8.1 Windows UIPI (Elevated Admin App Boundaries)
* **The Problem**: On Windows, User Interface Privilege Isolation (UIPI) prevents a non-elevated application from injecting `SendInput` keystrokes into an elevated process (e.g., Administrator Command Prompt, elevated POS systems, Task Manager).
* **OpenScanSim Solution**:
  * Shipped with an application manifest supporting `requireAdministrator` or `highestAvailable`.
  * Real-time detection: Queries `GetForegroundWindow()` and checks if target process has higher integrity level; alerts user in the HUD if keystroke injection might be blocked.

### 8.2 CapsLock, Active Modifiers & IME Swallow Immunity
* **The Problem**: If a user has `CapsLock` ON or is holding `Shift`, sending raw virtual keycodes (`VK_A`) will result in lower/uppercase inversion. In Asian Windows locales (Japanese, Chinese, Korean), raw keystrokes can trigger IME composition bars and fail to enter text.
* **OpenScanSim Solution**:
  * Uses `KEYEVENTF_UNICODE` (`wScan = char, dwFlags = 0x0004`) which bypasses keyboard layout mapping, CapsLock states, and IME character composition completely.

### 8.3 Windows Timer Resolution & `timeBeginPeriod(1)`
* **The Problem**: Default Windows timer precision (`Thread.Sleep`) is coarse (~15.6ms), causing erratic jitter when trying to simulate high-speed 1ms–2ms scanner bursts.
* **OpenScanSim Solution**:
  * Calls Win32 `timeBeginPeriod(1)` on startup to lock system timer resolution to 1ms.
  * Uses `Stopwatch.GetTimestamp()` with spin-wait for sub-millisecond precision.
  * Restores `timeEndPeriod(1)` gracefully on exit.

### 8.4 UHF Multi-Tag Collisions & Session Flag Decay
* **The Problem**: UHF portal readers reading a pallet with 300 boxes read tags asynchronously with backscatter noise, missed reads, and RSSI fluctuations.
* **OpenScanSim Solution**:
  * **Tag Collision Simulator**: Injects random read order shuffling and randomized RSSI attenuation ($-40\text{ dBm}$ to $-78\text{ dBm}$).
  * **EPC Gen2 Session 0/1/2/3 Emulation**: Simulates inventoried flags ($A \rightarrow B$) and tag persistence decay.

### 8.5 Target Window Locking (`HWND`) vs Active Focus
* **Active Mode**: Injects keystrokes into whichever window currently has focus.
* **Locked Target Window Mode (Optional)**: User can pick a specific application from a running process list. OpenScanSim monitors the window handle (`HWND`) via `GetForegroundWindow()` and automatically **pauses** if the user switches away, resuming when focus returns.

---

## 9. Cross-Platform Native Input Abstraction

```csharp
public interface IKeyboardSimulator
{
    Task SendKeystrokesAsync(string text, KeystrokeProfile profile, CancellationToken ct);
    Task SendKeyCombinationAsync(VirtualKey key, KeyModifiers modifiers, CancellationToken ct);
}
```

* **Windows**: `Win32KeyboardSimulator` using `SendInput` (User32.dll).
* **Linux**: `LinuxUInputKeyboardSimulator` using `/dev/uinput` kernel module or `XTestFakeKeyEvent` on X11.
* **macOS**: `MacOsQuartzKeyboardSimulator` using `CGEventCreateKeyboardEvent` / `CGEventPost(kCGHIDEventTap)`.

---

## 10. UI/UX Design, Visual Mockups & Component Tour

All user interfaces strictly adhere to the **Avalonia UI Fluent v2 Design System** (Windows 11 Dark Mode, `#202020` card surfaces, `1px #333333` subtle borders, Fluent NavigationRail, and `#0078D4` blue accent).

### 10.1 Main Control Dashboard (`ui_main_window.png`)
The primary application window features a Fluent Navigation Rail (Dashboard, Data Generator, Barcode & NFC Studio, Live Telemetry, Settings), reader protocol selectors, generator mask inputs, and keystroke wedge controls with native `SegmentedControl`, `Slider`, and `ToggleSwitch` elements.

![Main Control Dashboard](ui_main_window.png)

---

### 10.2 Floating Mini-HUD Widget (`ui_floating_hud.png`)
A compact `320 × 180 px` Avalonia acrylic floating widget equipped with `WS_EX_NOACTIVATE` focus safety, circular countdown ring (`2.1s`), live tag counter, active EPC monospace preview, and Fluent action buttons (`Pause F9`, `Stop F8`, `Skip F10`).

![Floating Mini HUD](ui_floating_hud.png)

---

### 10.3 Advanced Data Generator & Memory Bank Inspector (`ui_advanced_generator.png`)
The dedicated UHF RAIN RFID inspector displaying the Gen2 4-bank memory structure (Bank 00 Reserved, Bank 01 EPC with PC Word `3000` and live `CRC-16 OK` badge, Bank 10 TID, Bank 11 User Memory) alongside the GS1 SGTIN-96 Partition Table builder.

![Advanced Generator & Memory Inspector](ui_advanced_generator.png)

---

### 10.4 Barcode Symbology & NFC NDEF Studio (`ui_barcode_nfc_studio.png`)
A dedicated studio view featuring 1D/2D symbology configurators (Code 128, GS1 DataMatrix, QR Code with AIM ID prefixes `]C1`/`]Q1` and `FNC1 0x1D` separators) and a multi-record NFC NDEF Builder (URI, Text, MIME).

![Barcode Symbology & NFC NDEF Studio](ui_barcode_nfc_studio.png)

---

### 10.5 Live Simulation & Telemetry Monitor (`ui_live_telemetry_monitor.png`)
Real-time diagnostic view with a live Tags/Second (`450 TPS`) throughput curve, active queue status bar, high-speed scancode hex event stream, and Windows 11 System Tray context menu with quick-scan triggers.

![Live Simulation & Telemetry Monitor](ui_live_telemetry_monitor.png)

---

## 11. Codebase Standards & Clean Architecture Conventions

To ensure industrial-grade software engineering, maintainability, and effortless contribution by the open-source community, the codebase enforces strict coding standards.

### 11.1 Structural Layering (Clean Architecture)
```
┌────────────────────────────────────────────────────────────────────────┐
│                        OpenScanSim.UI (Presentation)                   │
│   Views (XAML), ViewModels (CommunityToolkit.Mvvm), Styles, Behaviors  │
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                     OpenScanSim.Platform (OS Abstraction)              │
│   Windows (SendInput/Win32), Linux (/dev/uinput), macOS (Quartz Events)│
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                       OpenScanSim.Core (Domain Engine)                 │
│   Generators, Validators, Timing Scheduler, Audio Synth, Audio Feedback│
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                      OpenScanSim.Common (Shared Primitives)            │
│   Models, DTOs, Enums, Constants, Zero-Allocation Utilities, Extensions│
└────────────────────────────────────────────────────────────────────────┘
```

### 11.2 Service, Config, Model, Util & Constant Conventions

#### 1. Models & Entities (`record` and `readonly struct`)
* All data records are immutable `record` or `readonly struct` to avoid accidental mutation across async pipeline boundaries.
* Example:
  ```csharp
  public readonly record struct ScanRecord(
      ReaderType ReaderType,
      string PrimaryPayload,
      string? ProtocolControlWord,
      string? Tid,
      string? UserMemoryHex,
      DateTime Timestamp,
      int SequenceIndex);
  ```

#### 2. Domain Services & Interfaces (`ISingleResponsibility`)
* Every business capability lives behind a dedicated interface registered in the Dependency Injection container:
  * `IScanDataGenerator` (Data generation algorithms)
  * `IProtocolValidator` (Modulo, CRC, GS1 validation)
  * `IKeyboardSimulator` (OS keystroke wedge driver)
  * `ISimulationOrchestrator` (Timing & streaming state machine)
  * `IAudioFeedbackService` (Scanner beep sound synthesizer)
  * `IProfileStorageService` (JSON profile serializer/deserializer)
  * `IProcessTracker` (Target window HWND monitor & UIPI boundary check)

#### 3. Strongly-Typed Configuration (`IOptions<T>`)
* All configurations are strongly-typed C# classes validated on startup:
  * `GeneratorConfig` (Mask, quantity, sequence offset)
  * `TimingConfig` (Countdown seconds, interval seconds, burst batch size)
  * `WedgeConfig` (Device vs human mode, typing speed, jitter, prefix/suffix)
  * `WindowConfig` (Topmost, snap behavior, opacity, tray minimize)

#### 4. Centralized Constants
* Zero hardcoded magic numbers or strings. All constants are grouped logically in `OpenScanSim.Common.Constants`:
  ```csharp
  public static class AppConstants
  {
      public static class Win32
      {
          public const int InputKeyboard = 1;
          public const uint KeyEventFUnicode = 0x0004;
          public const uint KeyEventFKeyUp = 0x0002;
          public const int WsExNoActivate = 0x08000000;
          public const int WsExTopmost = 0x00000008;
      }

      public static class Protocols
      {
          public const string DefaultEpcHeader = "30";
          public const string DefaultPcWord96Bit = "3000";
          public const char Gs1Fnc1Separator = '\x1D';
          public const string AimCodeGs1128 = "]C1";
          public const string AimCodeQrCode = "]Q1";
          public const string AimCodeDataMatrix = "]d2";
      }

      public static class Hotkeys
      {
          public const int StartStopHotkeyId = 9001;
          public const int PauseResumeHotkeyId = 9002;
          public const int SingleStepHotkeyId = 9003;
      }
  }
  ```

#### 5. Zero-Allocation Span Utilities
* String manipulation in the hot streaming loop uses `ReadOnlySpan<char>`, `Span<char>`, `string.Create()`, and `ArrayPool<byte>` to guarantee **0 Bytes/sec heap allocations during active simulation runs**.

---

## 12. Quality Control (QC), 4-Tier Testing & AI Verification

Quality assurance is built into every phase of OpenScanSim through a rigorous 4-tier testing framework, automated benchmarking, and autonomous AI-driven real-world verification.

```
┌────────────────────────────────────────────────────────────────────────┐
│             TIER 4: Autonomous AI Real-World Browser Verification      │
│  AI Agent tests live keystroke wedge typing into actual web app inputs │
├────────────────────────────────────────────────────────────────────────┤
│             TIER 3: System & Hardware Simulation Integration           │
│  Win32 message queue listeners, scancode hooks, window focus recovery  │
├────────────────────────────────────────────────────────────────────────┤
│             TIER 2: Pipeline Streaming & Allocation Benchmarks         │
│  10M record IAsyncEnumerable throughput, BenchmarkDotNet zero-alloc    │
├────────────────────────────────────────────────────────────────────────┤
│             TIER 1: Unit Testing (Mathematical & Protocol Rules)       │
│  CRC-16, Modulo 10/43 check digits, GS1 partition tables, NDEF builder │
└────────────────────────────────────────────────────────────────────────┘
```

### 12.1 Tier 1: Unit Testing (xUnit + FluentAssertions)
* **Mathematical Check Digits**: Tests 100+ standard test vectors for EAN-13, UPC-A, Modulo 10, Modulo 43, and CRC-16/CCITT polynomials.
* **GS1 SGTIN-96 Partition Tables**: Tests bit-level binary compaction for partitions 0 through 6 with varied Company Prefix and Item Reference lengths.
* **NFC NDEF Binary Encoders**: Asserts byte-level accuracy for URI records, Text status bytes, and MIME encapsulation.
* **Mask Syntax Parser**: Tests token extraction for `{HEX:8}`, `{SEQ:6}`, `{DATE:yyMMdd}`, and `{UUID}` patterns.

### 12.2 Tier 2: Integration & Stream Throughput Testing
* **Memory & Throughput Benchmarking (BenchmarkDotNet)**:
  * Verifies that generating 1,000,000 records achieves `> 100,000 items/sec` throughput with `< 100 KB` total allocated memory.
* **Memory-Mapped File (MMF) Stream Test**: Verifies sequential parsing of a 5,000,000-line test CSV without loading the file into RAM.
* **Pipeline Cancellation & Backpressure**: Tests immediate and clean aborts when `CancellationToken.Cancel()` is called mid-stream.

### 12.3 Tier 3: System & Hardware Wedge Testing
* **Virtual Windows Message Hook**: Injects keystrokes into a mock headless Win32 target message queue and asserts accurate scancodes, `KEYEVENTF_UNICODE` headers, and `Enter`/`Tab` suffixes.
* **Focus Switch & Pause Handler**: Simulates switching active window away from target application and asserts that simulation automatically pauses within 50ms.

### 12.4 Tier 4: Autonomous AI Real-World Browser Verification
To guarantee that OpenScanSim works in real-world web environments (e.g. Warehouse Portals, POS Web Apps, Library Management GUIs):
1. **Automated Subagent Setup**:
   * An autonomous AI agent launches a lightweight local test web page containing barcode/RFID listener fields with event listeners for `keydown`, `input`, and `change`.
2. **Execution & Hotkey Simulation**:
   * The AI agent clicks the web input field, triggers the simulation countdown via `F8`, and waits for the virtual wedge to type.
3. **DOM & Latency Verification**:
   * The agent validates that the input field receives the exact generated EPC/Barcode payload, that the `Enter` event is dispatched, and that the timing interval (e.g. 2.5s) between successive inputs is strictly preserved.

### 12.5 Automated Testing Report Generation
Every test run compiles an exhaustive markdown and HTML QC report (`TestExecutionReport.md`) detailing:
* Total test cases executed, passed, failed, and skipped.
* Execution throughput (Tags Per Second - TPS).
* Peak RAM footprint during 10M record stress runs.
* Full diagnostic logs for any failed scancode injections.

---

---

## 13. C# Solution Architecture & Class Implementation Details

```
OpenScanSim/
├── OpenScanSim.sln
├── src/
│   ├── OpenScanSim.Common/                    # Shared DTOs, Enums, Constants, Zero-alloc utilities
│   │   ├── Enums/                             # ReaderType, SimulationMode, OutputTerminator
│   │   ├── Models/                            # Immutable scan record struct, metadata
│   │   ├── Constants/                         # AppConstants.Protocols, AppConstants.Win32, etc.
│   │   └── Extensions/                        # SpanExtensions, HexFormatter
│   │
│   ├── OpenScanSim.Core/                      # Domain business logic & services
│   │   ├── Generators/                        # Sequential, Mask, MMF stream generators
│   │   ├── Validators/                        # Check digit, CRC-16, GS1 partition validators
│   │   └── Services/                          # SimulationOrchestrator, HighResTimer, AudioFeedback
│   │
│   ├── OpenScanSim.Platform.Windows/          # Windows Win32 SendInput & RegisterHotKey P/Invoke
│   ├── OpenScanSim.Platform.Linux/            # Linux /dev/uinput & XTest implementation
│   ├── OpenScanSim.Platform.MacOS/            # macOS CoreGraphics Quartz event injector
│   │
│   ├── OpenScanSim.UI/                        # Avalonia UI Fluent v2 presentation layer
│   │   ├── ViewModels/                        # MainViewModel, GeneratorViewModel, FloatingHudViewModel
│   │   ├── Views/                             # MainWindow.axaml, FloatingHudWindow.axaml
│   │   └── Styles/                            # Windows 11 Fluent dark theme tokens
│   │
│   └── OpenScanSim.Cli/                       # Headless CLI runner for CI/CD automation
│
└── tests/
    ├── OpenScanSim.UnitTests/                 # Mathematical & protocol vector unit tests
    ├── OpenScanSim.IntegrationTests/          # Stream pipeline & MMF file tests
    ├── OpenScanSim.SystemTests/               # Virtual Win32 wedge & hotkey system tests
    └── OpenScanSim.Benchmarks/                # BenchmarkDotNet zero-allocation throughput benchmarks
```

---

## 14. Open-Source Roadmap, Milestones & Contribution

```
  MILESTONE 1 (v0.1 - MVP)  ──►  MILESTONE 2 (v0.5 - Suite)  ──►  MILESTONE 3 (v1.0 - Full)
  • C# .NET 9 + Avalonia UI      • Full UHF Banks (TID, User)      • Linux (/dev/uinput)
  • Barcode & UHF EPC 96-bit     • NFC NDEF Builder                • macOS (CGEvent)
  • Zero-RAM Lazy Generator      • Human Typing Jitter Engine      • Virtual COM / TCP Socket
  • Fast SendInput Keystroke     • Floating HUD & Global Hotkeys   • Headless CLI Runner
```

* **License**: **MIT License** / **Apache 2.0**.
* **Repository**: Open-source on GitHub.

---

## 15. Appendices & Reference Implementations

### Appendix A: Simulation Profile Configuration Schema (`.oscan` / `profile.json`)
Allows saving, exporting, and sharing simulation profiles across development teams:

```json
{
  "$schema": "https://openscansim.org/schemas/v1/profile.json",
  "profileName": "Retail UHF SGTIN-96 Pallet Stream",
  "version": "1.0",
  "reader": {
    "type": "UhfRfid",
    "encoding": "Sgtin96",
    "banks": {
      "epc": { "enabled": true, "pcWord": "3000" },
      "tid": { "enabled": true, "model": "ImpinjMonzaR6" },
      "userMemory": { "enabled": false }
    }
  },
  "generator": {
    "mode": "TemplateMask",
    "maskPattern": "3034{HEX:8}{SEQ:8}",
    "startSequence": 1,
    "totalCount": 10000000,
    "seed": null
  },
  "wedge": {
    "mode": "FastDeviceBurst",
    "typingSpeedMs": 0,
    "jitterMs": 0,
    "prefix": "None",
    "suffix": "Enter",
    "bankSeparator": "|",
    "audioFeedback": true
  },
  "timing": {
    "startCountdownSeconds": 5.0,
    "intervalSeconds": 2.5,
    "burstBatchSize": 1,
    "loopContinuously": false
  },
  "window": {
    "alwaysOnTop": true,
    "autoCollapseToMiniHud": true,
    "minimizeToTray": true,
    "targetProcessName": null
  }
}
```

---

### Appendix B: Headless CLI Runner Syntax (`OpenScanSim.Cli`)
Enables automated testing in headless CI/CD environments and shell scripts:

```bash
# Stream 500 UHF EPCs with 5s countdown and 2s interval
dotnet run --project src/OpenScanSim.Cli -- --reader uhf --mask "3034{HEX:8}{SEQ:8}" --count 500 --interval 2.0 --start-delay 5.0 --suffix enter

# Stream 1,000 EAN-13 barcodes in human typing simulation mode
dotnet run --project src/OpenScanSim.Cli -- --reader barcode --symbology ean13 --mode human --typing-speed 120 --count 1000

# Run directly from a saved profile file
dotnet run --project src/OpenScanSim.Cli -- --profile ./profiles/warehouse_pallet.oscan
```

---

### Appendix C: Complete Win32 P/Invoke Native Definitions

```csharp
namespace OpenScanSim.Platform.Windows.Native;

internal static class User32
{
    public const int INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_UNICODE = 0x0004;
    public const uint KEYEVENTF_SCANCODE = 0x0008;

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOPMOST = 0x00000008;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
    public static extern uint TimeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
    public static extern uint TimeEndPeriod(uint uMilliseconds);
}
```

---

### Appendix D: Implementation Sprint Breakdown

| Sprint | Goal | Deliverables |
| :--- | :--- | :--- |
| **Sprint 1** | **Solution & Core Engine** | Setup .NET 9 solution, `OpenScanSim.Core`, `IAsyncEnumerable` generators (Sequential, Mask, MMF file streamer), and checksum validators (CRC-16, Modulo 10). |
| **Sprint 2** | **Native Windows Wedge & Timer** | `OpenScanSim.Platform.Windows`, `SendInput` batch injector, `KEYEVENTF_UNICODE`, Gaussian jitter human engine, multimedia 1ms timer, and global hotkeys. |
| **Sprint 3** | **Avalonia UI Dashboard & Styles** | Fluent v2 dark theme, `MainWindow.axaml`, reader tabs, real-time hex payload preview, and MVVM ViewModels. |
| **Sprint 4** | **Floating Mini-HUD & Tray Daemon** | Compact `320x180px` HUD window with `WS_EX_NOACTIVATE` focus safety, circular countdown arc, and System Tray context menu. |
| **Sprint 5** | **Barcode & NFC Studio + Telemetry** | Symbology settings, AIM prefixes, FNC1 group separators, NFC NDEF builder, and live Throughput (TPS) telemetry view. |
| **Sprint 6** | **Packaging, Tests & Documentation** | Automated unit tests (xUnit), BenchmarkDotNet throughput validation, single-file `.exe` publishing, and GitHub release pipeline. |

---
*Created for OpenScanSim Project Development.*

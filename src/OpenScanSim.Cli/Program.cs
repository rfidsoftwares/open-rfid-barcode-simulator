using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenScanSim.Common.Constants;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Generators;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Services;
using OpenScanSim.Core.Validators;
using OpenScanSim.Platform.Windows;

namespace OpenScanSim.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintHelp();
            return 0;
        }

        if (args[0] is "-v" or "--version" or "version")
        {
            PrintBanner();
            return 0;
        }

        string command = args[0].ToLowerInvariant();
        string[] cmdArgs = args.Length > 1 ? args[1..] : [];

        try
        {
            return command switch
            {
                "simulate" => await RunSimulateAsync(cmdArgs),
                "generate" => await RunGenerateAsync(cmdArgs),
                "validate" => RunValidate(cmdArgs),
                _ => HandleUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL ERROR] {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
   ___                    ____  _____ ___ ____  
  / _ \ _ __   ___ _ __  |  _ \|  ___|_ _|  _ \ 
 | | | | '_ \ / _ \ '_ \ | |_) | |_   | || | | |
 | |_| | |_) |  __/ | | ||  _ <|  _|  | || |_| |
  \___/| .__/ \___|_| |_||_| \_\_|   |___|____/ 
       |_|                                      
 OpenRFID & Barcode Simulator - Headless Wedge CLI v1.0.0
 Sponsored by RFID Softwares (https://rfidsoftwares.com/) - MIT License");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintHelp()
    {
        PrintBanner();
        Console.WriteLine(@"USAGE:
  openscansim <command> [options]

COMMANDS:
  simulate    Stream live keystroke scans into active window
  generate    Generate synthetic tag/barcode payloads to file or stdout
  validate    Validate hex payloads, EPC lengths, and barcode check digits
  version     Display engine version and build information

EXAMPLES:
  # Simulate 100 UHF RFID tag reads at fast burst speed (<8ms per tag)
  openscansim simulate --mode uhf --mask ""3034{HEX:4}{SEQ:4}"" --count 100 --speed burst --countdown 3

  # Simulate 20 EAN-13 barcodes with human typing jitter
  openscansim simulate --mode barcode --symbology ean13 --data ""400638133393"" --count 20 --speed human

  # Generate 50,000 EPC tags to CSV file
  openscansim generate --mask ""3034{HEX:4}{SEQ:4}"" --count 50000 --output tags.csv

  # Validate EAN-13 Barcode Check Digit
  openscansim validate --type barcode --symbology ean13 --data ""4006381333931""
");
    }

    private static async Task<int> RunSimulateAsync(string[] args)
    {
        PrintBanner();

        ReaderType reader = ReaderType.UhfRfid;
        BarcodeSymbology symbology = BarcodeSymbology.Code128;
        string mask = AppConstants.Defaults.DefaultUhfMask;
        ulong count = 10;
        ulong startSeq = 1;
        SimulationMode mode = SimulationMode.FastDeviceBurst;
        double countdown = 3.0;
        double interval = 0.1;
        bool audio = true;
        OutputTerminator terminator = OutputTerminator.Enter;
        string prefix = "";
        string? data = null;

        for (int i = 0; i < args.Length; i++)
        {
            string flag = args[i].ToLowerInvariant();
            string NextVal() => (i + 1 < args.Length) ? args[++i] : "";

            switch (flag)
            {
                case "--mode":
                case "-m":
                    string m = NextVal().ToLowerInvariant();
                    reader = m switch
                    {
                        "barcode" => ReaderType.Barcode1D2D,
                        "hf" => ReaderType.HfRfid,
                        "nfc" => ReaderType.Nfc,
                        _ => ReaderType.UhfRfid
                    };
                    break;

                case "--symbology":
                    string s = NextVal().ToLowerInvariant();
                    symbology = s switch
                    {
                        "ean13" => BarcodeSymbology.Ean13,
                        "upca" => BarcodeSymbology.UpcA,
                        "code39" => BarcodeSymbology.Code39,
                        "qrcode" => BarcodeSymbology.QrCode,
                        "datamatrix" => BarcodeSymbology.DataMatrix,
                        _ => BarcodeSymbology.Code128
                    };
                    break;

                case "--mask":
                    mask = NextVal();
                    break;

                case "--data":
                    data = NextVal();
                    break;

                case "--count":
                case "-c":
                    if (ulong.TryParse(NextVal(), out var c)) count = c;
                    break;

                case "--start":
                    if (ulong.TryParse(NextVal(), out var seq)) startSeq = seq;
                    break;

                case "--speed":
                    string spd = NextVal().ToLowerInvariant();
                    mode = spd == "human" ? SimulationMode.HumanTyping : SimulationMode.FastDeviceBurst;
                    break;

                case "--countdown":
                    if (double.TryParse(NextVal(), out var cd)) countdown = cd;
                    break;

                case "--interval":
                    if (double.TryParse(NextVal(), out var iv)) interval = iv;
                    break;

                case "--prefix":
                    prefix = NextVal();
                    break;

                case "--suffix":
                    string suf = NextVal().ToLowerInvariant();
                    terminator = suf switch
                    {
                        "tab" => OutputTerminator.Tab,
                        "space" => OutputTerminator.Space,
                        "none" => OutputTerminator.None,
                        _ => OutputTerminator.Enter
                    };
                    break;

                case "--beep":
                    string bp = NextVal().ToLowerInvariant();
                    audio = bp is "true" or "1" or "yes";
                    break;
            }
        }

        if (reader == ReaderType.Barcode1D2D && !string.IsNullOrEmpty(data))
        {
            mask = data;
        }

        var genConfig = new GeneratorConfig
        {
            ReaderType = reader,
            Symbology = symbology,
            MaskPattern = mask,
            TotalCount = count,
            StartSequence = startSeq
        };

        var timingConfig = new TimingConfig
        {
            StartCountdownSeconds = countdown,
            IntervalSeconds = interval,
            BurstBatchSize = 1
        };

        var wedgeConfig = new WedgeConfig
        {
            Mode = mode,
            Prefix = prefix,
            Terminator = terminator,
            AudioFeedbackEnabled = audio
        };

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[INIT] Reader: {reader} | Mode: {mode} | Batch: {count} scans | Interval: {interval}s");
        if (countdown > 0)
        {
            Console.WriteLine($"[FOCUS] Starting in {countdown:F1}s... Click target application window NOW!");
        }
        Console.ResetColor();

        MaskPatternGenerator generator = new();
        Win32KeyboardSimulator keyboard = new();
        AudioFeedbackService audioService = new();
        SimulationOrchestrator orchestrator = new(generator, keyboard, audioService);

        orchestrator.ScanTransmitted += r =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[{DateTime.UtcNow:HH:mm:ss.fff}] ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"#{r.SequenceIndex:D4} ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{r.PrimaryPayload}");
            Console.ResetColor();
        };

        orchestrator.ProgressChanged += p =>
        {
            if (p.State == SimulationState.StartingCountdown)
            {
                Console.Write($"\r[COUNTDOWN] {p.CountdownRemainingSeconds:F1}s remaining...   ");
            }
        };

        await orchestrator.StartAsync(genConfig, timingConfig, wedgeConfig);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[DONE] Simulation batch successfully completed.");
        Console.ResetColor();
        return 0;
    }

    private static async Task<int> RunGenerateAsync(string[] args)
    {
        string mask = AppConstants.Defaults.DefaultUhfMask;
        ulong count = 1000;
        ulong startSeq = 1;
        string? outputFile = null;

        for (int i = 0; i < args.Length; i++)
        {
            string flag = args[i].ToLowerInvariant();
            string NextVal() => (i + 1 < args.Length) ? args[++i] : "";

            switch (flag)
            {
                case "--mask":
                case "-m":
                    mask = NextVal();
                    break;
                case "--count":
                case "-c":
                    if (ulong.TryParse(NextVal(), out var c)) count = c;
                    break;
                case "--start":
                    if (ulong.TryParse(NextVal(), out var seq)) startSeq = seq;
                    break;
                case "--output":
                case "-o":
                    outputFile = NextVal();
                    break;
            }
        }

        var config = new GeneratorConfig
        {
            ReaderType = ReaderType.UhfRfid,
            MaskPattern = mask,
            TotalCount = count,
            StartSequence = startSeq
        };

        MaskPatternGenerator generator = new();

        if (string.IsNullOrEmpty(outputFile))
        {
            await foreach (var item in generator.GenerateStreamAsync(config))
            {
                Console.WriteLine(item.PrimaryPayload);
            }
        }
        else
        {
            Console.WriteLine($"[GENERATE] Writing {count:N0} payloads to {outputFile}...");
            using var writer = new StreamWriter(outputFile, false, Encoding.UTF8);
            await writer.WriteLineAsync("Sequence,Payload");

            await foreach (var item in generator.GenerateStreamAsync(config))
            {
                await writer.WriteLineAsync($"{item.SequenceIndex},{item.PrimaryPayload}");
            }
            Console.WriteLine($"[COMPLETE] File written successfully.");
        }

        return 0;
    }

    private static int RunValidate(string[] args)
    {
        ReaderType reader = ReaderType.UhfRfid;
        BarcodeSymbology symbology = BarcodeSymbology.Code128;
        string data = "";

        for (int i = 0; i < args.Length; i++)
        {
            string flag = args[i].ToLowerInvariant();
            string NextVal() => (i + 1 < args.Length) ? args[++i] : "";

            switch (flag)
            {
                case "--type":
                case "-t":
                    string t = NextVal().ToLowerInvariant();
                    reader = t switch
                    {
                        "barcode" => ReaderType.Barcode1D2D,
                        "hf" => ReaderType.HfRfid,
                        "nfc" => ReaderType.Nfc,
                        _ => ReaderType.UhfRfid
                    };
                    break;

                case "--symbology":
                case "-s":
                    string s = NextVal().ToLowerInvariant();
                    symbology = s switch
                    {
                        "ean13" => BarcodeSymbology.Ean13,
                        "upca" => BarcodeSymbology.UpcA,
                        "code39" => BarcodeSymbology.Code39,
                        "qrcode" => BarcodeSymbology.QrCode,
                        "datamatrix" => BarcodeSymbology.DataMatrix,
                        _ => BarcodeSymbology.Code128
                    };
                    break;

                case "--data":
                case "-d":
                    data = NextVal();
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(data))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR] Missing required --data argument.");
            Console.ResetColor();
            return 1;
        }

        ProtocolValidator validator = new();
        var result = validator.Validate(reader, data, symbology);

        if (result.IsValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[VALID] Payload '{data}' conforms to {reader} standard.");
            Console.ResetColor();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[INVALID] {result.ErrorMessage}");
            Console.ResetColor();
            return 1;
        }
    }

    private static int HandleUnknownCommand(string command)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] Unknown command '{command}'. Run 'openscansim help' for available commands.");
        Console.ResetColor();
        return 1;
    }
}

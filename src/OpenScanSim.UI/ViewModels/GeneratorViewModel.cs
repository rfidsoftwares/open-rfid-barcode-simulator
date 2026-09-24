using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenScanSim.Common.Constants;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Generators;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class GeneratorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _maskPattern = AppConstants.Defaults.DefaultUhfMask;

    [ObservableProperty]
    private ulong _totalCount = 100;

    [ObservableProperty]
    private ulong _startSequence = 1;

    [ObservableProperty]
    private BarcodeSymbology _selectedSymbology = BarcodeSymbology.Code128;

    [ObservableProperty]
    private bool _includeCheckDigit = true;

    [ObservableProperty]
    private bool _includeAimPrefix = false;

    [ObservableProperty]
    private bool _includeTid = false;

    [ObservableProperty]
    private bool _includeUserMemory = false;

    // Generation Modes: 0 = Mask Template, 1 = Direct Pasted List, 2 = File Stream
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMaskMode))]
    [NotifyPropertyChangedFor(nameof(IsPastedMode))]
    [NotifyPropertyChangedFor(nameof(IsFileStreamMode))]
    private int _dataInputMode = 0;

    public bool IsMaskMode => DataInputMode == 0;
    public bool IsPastedMode => DataInputMode == 1;
    public bool IsFileStreamMode => DataInputMode == 2;

    [ObservableProperty]
    private string _pastedListText = string.Empty;

    [ObservableProperty]
    private int _pastedItemCount = 0;

    [ObservableProperty]
    private string _pastedListSummary = "0 tags / barcodes in queue";

    [ObservableProperty]
    private string _sourceFilePath = string.Empty;

    [ObservableProperty]
    private bool _useFileStream = false;

    [ObservableProperty]
    private string _previewPayload = string.Empty;

    public GeneratorViewModel()
    {
        UpdatePreview();
    }

    partial void OnDataInputModeChanged(int value)
    {
        if (value == 1 && PastedItemCount > 0)
        {
            TotalCount = (ulong)PastedItemCount;
        }
        UpdatePreview();
    }

    partial void OnMaskPatternChanged(string value) => UpdatePreview();
    partial void OnStartSequenceChanged(ulong value) => UpdatePreview();

    partial void OnPastedListTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            PastedItemCount = 0;
            PastedListSummary = "0 items in queue";
        }
        else
        {
            var lines = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            PastedItemCount = lines.Length;
            PastedListSummary = $"{lines.Length:N0} direct IDs loaded in queue";

            // If user pastes items and is in direct pasted mode, auto-set batch count
            if (DataInputMode == 1 && lines.Length > 0)
            {
                TotalCount = (ulong)lines.Length;
            }
        }
        UpdatePreview();
    }

    public void UpdatePreview()
    {
        try
        {
            if (DataInputMode == 1 && !string.IsNullOrWhiteSpace(PastedListText))
            {
                var lines = PastedListText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (lines.Length > 0)
                {
                    PreviewPayload = lines[0] + (lines.Length > 1 ? $"  (+ {lines.Length - 1} more items)" : "");
                    return;
                }
            }

            string pattern = string.IsNullOrWhiteSpace(MaskPattern) ? AppConstants.Defaults.DefaultUhfMask : MaskPattern;
            PreviewPayload = MaskPatternGenerator.EvaluatePattern(pattern, StartSequence, Random.Shared);
        }
        catch (Exception ex)
        {
            PreviewPayload = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    public void SelectMaskMode() => DataInputMode = 0;

    [RelayCommand]
    public void SelectPastedMode() => DataInputMode = 1;

    [RelayCommand]
    public void SelectFileMode() => DataInputMode = 2;

    [RelayCommand]
    public void LoadSamplePastedList()
    {
        PastedListText = string.Join(Environment.NewLine, new[]
        {
            "E28033000F000F0F3BC9B222",
            "E28033000F00050A6847166C",
            "E28033000F000102B7C62064",
            "E28033000F000907990E3DCB",
            "E28033000F00040EF26F5DDF",
            "E28033000F0011028E7D08DB",
            "E28033000F000708B0532BF3",
            "E28033000F001005A8812FD0",
            "E28033000F000B04C37BA15A",
            "E28033000F00020109064B1F",
            "E28033000F0008036B6DA5AE",
            "E28033000F000E0032F13A3E",
            "E28033000F001404B503F991",
            "E28033000F00130DADC4A171",
            "E28033000F000D0160E6582E",
            "E28033000F00030ACAC225E3",
            "E28033000F0006085F40ADC5",
            "E28033000F000A0920F258A1",
            "E28033000F00120785D5D093",
            "E28033000F000C089741B578"
        });
        DataInputMode = 1;
    }

    [RelayCommand]
    public void ClearPastedList()
    {
        PastedListText = string.Empty;
    }

    public GeneratorConfig BuildGeneratorConfig(ReaderType readerType)
    {
        bool isPasted = DataInputMode == 1 && !string.IsNullOrWhiteSpace(PastedListText);

        return new GeneratorConfig
        {
            ReaderType = readerType,
            MaskPattern = MaskPattern,
            TotalCount = TotalCount,
            StartSequence = StartSequence,
            Symbology = SelectedSymbology,
            IncludeCheckDigit = IncludeCheckDigit,
            IncludeAimPrefix = IncludeAimPrefix,
            IncludeTid = IncludeTid,
            IncludeUserMemory = IncludeUserMemory,
            SourceFilePath = (DataInputMode == 2 || UseFileStream) ? SourceFilePath : null,
            DirectPastedList = isPasted ? PastedListText : null
        };
    }
}

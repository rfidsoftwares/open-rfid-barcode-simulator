using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Extensions;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Services;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class NdefStudioViewModel : ViewModelBase
{
    private readonly IKeyboardSimulator _keyboard;
    private readonly IAudioFeedbackService _audio;
    private readonly TimingViewModel _timingVm;

    [ObservableProperty]
    private int _recordTypeIndex = 0; // 0: URI, 1: Text

    [ObservableProperty]
    private string _uriInput = "https://rfidsoftwares.com/";

    [ObservableProperty]
    private string _textInput = "OpenRFID Studio NFC Tag";

    [ObservableProperty]
    private string _languageCode = "en";

    [ObservableProperty]
    private string _hexOutput = string.Empty;

    [ObservableProperty]
    private int _byteLength;

    [ObservableProperty]
    private string _binaryRepresentation = string.Empty;

    public NdefStudioViewModel(
        IKeyboardSimulator keyboard,
        IAudioFeedbackService audio,
        TimingViewModel timingVm)
    {
        _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _timingVm = timingVm ?? throw new ArgumentNullException(nameof(timingVm));

        UpdateNdef();
    }

    partial void OnRecordTypeIndexChanged(int value) => UpdateNdef();
    partial void OnUriInputChanged(string value) => UpdateNdef();
    partial void OnTextInputChanged(string value) => UpdateNdef();
    partial void OnLanguageCodeChanged(string value) => UpdateNdef();

    public void UpdateNdef()
    {
        try
        {
            byte[] recordBytes = RecordTypeIndex switch
            {
                0 => NdefMessageEncoder.CreateUriRecord(UriInput),
                _ => NdefMessageEncoder.CreateTextRecord(TextInput, LanguageCode)
            };

            ByteLength = recordBytes.Length;
            HexOutput = SpanExtensions.ToHexStringFast(recordBytes);
            BinaryRepresentation = BitConverter.ToString(recordBytes).Replace("-", " ");
        }
        catch (Exception ex)
        {
            HexOutput = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task InjectNdefAsync()
    {
        if (string.IsNullOrEmpty(HexOutput)) return;

        var wedgeConfig = _timingVm.BuildWedgeConfig();
        await _keyboard.SendKeystrokesAsync(HexOutput, wedgeConfig);

        if (wedgeConfig.AudioFeedbackEnabled)
        {
            _audio.PlayScanBeep(wedgeConfig.AudioPitchHz, wedgeConfig.AudioDurationMs);
        }
    }
}

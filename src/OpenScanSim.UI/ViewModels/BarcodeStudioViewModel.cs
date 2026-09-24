using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenScanSim.Common.Constants;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Services;
using OpenScanSim.Core.Validators;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class BarcodeStudioViewModel : ViewModelBase
{
    private readonly IKeyboardSimulator _keyboard;
    private readonly IAudioFeedbackService _audio;
    private readonly IProtocolValidator _validator;
    private readonly TimingViewModel _timingVm;

    [ObservableProperty]
    private BarcodeSymbology _symbology = BarcodeSymbology.Code128;

    [ObservableProperty]
    private string _rawPayload = "400638133393";

    [ObservableProperty]
    private bool _includeCheckDigit = true;

    [ObservableProperty]
    private bool _includeAimPrefix = false;

    [ObservableProperty]
    private string _formattedOutput = string.Empty;

    [ObservableProperty]
    private string _validationStatus = "Valid Payload";

    [ObservableProperty]
    private string _validationColor = "#10B981";

    public BarcodeStudioViewModel(
        IKeyboardSimulator keyboard,
        IAudioFeedbackService audio,
        IProtocolValidator validator,
        TimingViewModel timingVm)
    {
        _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timingVm = timingVm ?? throw new ArgumentNullException(nameof(timingVm));

        UpdateFormattedOutput();
    }

    partial void OnSymbologyChanged(BarcodeSymbology value) => UpdateFormattedOutput();
    partial void OnRawPayloadChanged(string value) => UpdateFormattedOutput();
    partial void OnIncludeCheckDigitChanged(bool value) => UpdateFormattedOutput();
    partial void OnIncludeAimPrefixChanged(bool value) => UpdateFormattedOutput();

    public void UpdateFormattedOutput()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(RawPayload))
            {
                FormattedOutput = string.Empty;
                ValidationStatus = "Payload is empty";
                ValidationColor = "#64748B";
                return;
            }

            FormattedOutput = BarcodeSymbologyEncoder.FormatBarcode(
                Symbology,
                RawPayload,
                IncludeCheckDigit,
                IncludeAimPrefix);

            var validation = _validator.Validate(ReaderType.Barcode1D2D, FormattedOutput, Symbology);
            if (validation.IsValid)
            {
                ValidationStatus = "✓ Formatted & Verified";
                ValidationColor = "#10B981";
            }
            else
            {
                ValidationStatus = $"⚠ {validation.ErrorMessage}";
                ValidationColor = "#F59E0B";
            }
        }
        catch (Exception ex)
        {
            FormattedOutput = $"Error: {ex.Message}";
            ValidationStatus = ex.Message;
            ValidationColor = "#EF4444";
        }
    }

    [RelayCommand]
    public async Task InjectSingleScanAsync()
    {
        if (string.IsNullOrEmpty(FormattedOutput)) return;

        var wedgeConfig = _timingVm.BuildWedgeConfig();
        await _keyboard.SendKeystrokesAsync(FormattedOutput, wedgeConfig);

        if (wedgeConfig.AudioFeedbackEnabled)
        {
            _audio.PlayScanBeep(wedgeConfig.AudioPitchHz, wedgeConfig.AudioDurationMs);
        }
    }
}

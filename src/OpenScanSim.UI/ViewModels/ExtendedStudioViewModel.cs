using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenScanSim.Common.Enums;
using OpenScanSim.Common.Models;
using OpenScanSim.Core.Protocols;
using OpenScanSim.Core.Services;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class ExtendedStudioViewModel : ViewModelBase
{
    private readonly IKeyboardSimulator _keyboard;
    private readonly IAudioFeedbackService _audio;
    private readonly TimingViewModel _timingVm;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    // Passport MRZ Fields
    [ObservableProperty]
    private string _passportCountry = "IND";

    [ObservableProperty]
    private string _passportSurname = "SHARMA";

    [ObservableProperty]
    private string _passportGivenNames = "AKASH";

    [ObservableProperty]
    private string _passportDocNumber = "Z9876543";

    [ObservableProperty]
    private string _passportNationality = "IND";

    [ObservableProperty]
    private DateTimeOffset _passportDob = new(1995, 8, 15, 0, 0, 0, TimeSpan.Zero);

    [ObservableProperty]
    private string _passportSex = "M";

    [ObservableProperty]
    private DateTimeOffset _passportExpiry = new(2035, 8, 14, 0, 0, 0, TimeSpan.Zero);

    [ObservableProperty]
    private string _passportPersonalNumber = "A123456789";

    [ObservableProperty]
    private string _mrzLine1 = "";

    [ObservableProperty]
    private string _mrzLine2 = "";

    [ObservableProperty]
    private string _mrzCombined = "";

    // Scale Wedge Fields
    [ObservableProperty]
    private DigitalScaleProtocol _selectedScaleProtocol = DigitalScaleProtocol.MettlerToledoContinuous;

    [ObservableProperty]
    private decimal _scaleWeight = 24.50m;

    [ObservableProperty]
    private decimal _scaleTare = 0.00m;

    [ObservableProperty]
    private string _scaleUnit = "kg";

    [ObservableProperty]
    private bool _isScaleStable = true;

    [ObservableProperty]
    private bool _isScaleGross = true;

    [ObservableProperty]
    private string _scaleFormattedOutput = "";

    // BLE Beacon Fields
    [ObservableProperty]
    private string _beaconUuid = "E2C56DB5-DFFB-48D2-B060-D0F5A71096E0";

    [ObservableProperty]
    private ushort _beaconMajor = 100;

    [ObservableProperty]
    private ushort _beaconMinor = 1;

    [ObservableProperty]
    private sbyte _beaconTxPower = -59;

    [ObservableProperty]
    private string _eddystoneUrl = "https://rfidsoftwares.com";

    [ObservableProperty]
    private string _beaconHexOutput = "";

    [ObservableProperty]
    private string _eddystoneHexOutput = "";

    [ObservableProperty]
    private string _statusFeedback = "Ready";

    public ExtendedStudioViewModel(
        IKeyboardSimulator keyboard,
        IAudioFeedbackService audio,
        TimingViewModel timingVm)
    {
        _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _timingVm = timingVm ?? throw new ArgumentNullException(nameof(timingVm));

        UpdateMrzPreview();
        UpdateScalePreview();
        UpdateBeaconPreview();
    }

    partial void OnPassportCountryChanged(string value) => UpdateMrzPreview();
    partial void OnPassportSurnameChanged(string value) => UpdateMrzPreview();
    partial void OnPassportGivenNamesChanged(string value) => UpdateMrzPreview();
    partial void OnPassportDocNumberChanged(string value) => UpdateMrzPreview();
    partial void OnPassportNationalityChanged(string value) => UpdateMrzPreview();
    partial void OnPassportDobChanged(DateTimeOffset value) => UpdateMrzPreview();
    partial void OnPassportSexChanged(string value) => UpdateMrzPreview();
    partial void OnPassportExpiryChanged(DateTimeOffset value) => UpdateMrzPreview();
    partial void OnPassportPersonalNumberChanged(string value) => UpdateMrzPreview();

    partial void OnSelectedScaleProtocolChanged(DigitalScaleProtocol value) => UpdateScalePreview();
    partial void OnScaleWeightChanged(decimal value) => UpdateScalePreview();
    partial void OnScaleTareChanged(decimal value) => UpdateScalePreview();
    partial void OnScaleUnitChanged(string value) => UpdateScalePreview();
    partial void OnIsScaleStableChanged(bool value) => UpdateScalePreview();
    partial void OnIsScaleGrossChanged(bool value) => UpdateScalePreview();

    partial void OnBeaconUuidChanged(string value) => UpdateBeaconPreview();
    partial void OnBeaconMajorChanged(ushort value) => UpdateBeaconPreview();
    partial void OnBeaconMinorChanged(ushort value) => UpdateBeaconPreview();
    partial void OnBeaconTxPowerChanged(sbyte value) => UpdateBeaconPreview();
    partial void OnEddystoneUrlChanged(string value) => UpdateBeaconPreview();

    [RelayCommand]
    private void UpdateMrzPreview()
    {
        try
        {
            char sex = !string.IsNullOrEmpty(PassportSex) ? PassportSex[0] : 'M';
            var (l1, l2, combined) = IcaoPassportMrzEncoder.EncodeTd3Passport(
                PassportCountry,
                PassportSurname,
                PassportGivenNames,
                PassportDocNumber,
                PassportNationality,
                PassportDob.DateTime,
                sex,
                PassportExpiry.DateTime,
                PassportPersonalNumber);

            MrzLine1 = l1;
            MrzLine2 = l2;
            MrzCombined = combined;
            StatusFeedback = "ICAO Doc 9303 TD3 Checksums Verified";
        }
        catch (Exception ex)
        {
            StatusFeedback = $"MRZ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void UpdateScalePreview()
    {
        try
        {
            ScaleFormattedOutput = DigitalScaleWedgeEncoder.FormatScaleReading(
                SelectedScaleProtocol,
                ScaleWeight,
                ScaleUnit,
                IsScaleStable,
                IsScaleGross,
                ScaleTare);
            StatusFeedback = $"Digital Scale Protocol: {SelectedScaleProtocol}";
        }
        catch (Exception ex)
        {
            StatusFeedback = $"Scale Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void UpdateBeaconPreview()
    {
        try
        {
            if (Guid.TryParse(BeaconUuid, out var guid))
            {
                var (_, hex) = BleBeaconWedgeEncoder.EncodeIBeacon(guid, BeaconMajor, BeaconMinor, BeaconTxPower);
                BeaconHexOutput = hex;
            }

            var (_, eddyHex) = BleBeaconWedgeEncoder.EncodeEddystoneUrl(EddystoneUrl);
            EddystoneHexOutput = eddyHex;
            StatusFeedback = "BLE Frames Encoded";
        }
        catch (Exception ex)
        {
            StatusFeedback = $"BLE Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task InjectMrzScanAsync()
    {
        if (string.IsNullOrEmpty(MrzCombined)) return;

        var wedgeConfig = _timingVm.BuildWedgeConfig();
        await _keyboard.SendKeystrokesAsync(MrzCombined, wedgeConfig);

        if (wedgeConfig.AudioFeedbackEnabled)
        {
            _audio.PlayScanBeep(wedgeConfig.AudioPitchHz, wedgeConfig.AudioDurationMs);
        }
        StatusFeedback = "⚡ Injected ICAO Passport MRZ to Active Cursor!";
    }

    [RelayCommand]
    private async Task InjectScaleScanAsync()
    {
        if (string.IsNullOrEmpty(ScaleFormattedOutput)) return;

        var wedgeConfig = _timingVm.BuildWedgeConfig();
        await _keyboard.SendKeystrokesAsync(ScaleFormattedOutput, wedgeConfig);

        if (wedgeConfig.AudioFeedbackEnabled)
        {
            _audio.PlayScanBeep(wedgeConfig.AudioPitchHz, wedgeConfig.AudioDurationMs);
        }
        StatusFeedback = "⚡ Injected Scale Reading to Active Cursor!";
    }

    [RelayCommand]
    private async Task InjectBeaconScanAsync()
    {
        if (string.IsNullOrEmpty(BeaconHexOutput)) return;

        var wedgeConfig = _timingVm.BuildWedgeConfig();
        await _keyboard.SendKeystrokesAsync(BeaconHexOutput, wedgeConfig);

        if (wedgeConfig.AudioFeedbackEnabled)
        {
            _audio.PlayScanBeep(wedgeConfig.AudioPitchHz, wedgeConfig.AudioDurationMs);
        }
        StatusFeedback = "⚡ Injected BLE Advertisement Frame to Active Cursor!";
    }
}

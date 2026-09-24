using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenScanSim.Core.Protocols;

namespace OpenScanSim.UI.ViewModels;

public sealed partial class InspectorViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _filterValue = 3;

    [ObservableProperty]
    private int _partition = 2;

    [ObservableProperty]
    private ulong _companyPrefix = 614141;

    [ObservableProperty]
    private ulong _itemReference = 100;

    [ObservableProperty]
    private ulong _serialNumber = 1;

    [ObservableProperty]
    private string _calculatedEpcHex = string.Empty;

    [ObservableProperty]
    private string _calculatedPcWord = "3000";

    [ObservableProperty]
    private string _calculatedCrc16 = string.Empty;

    // 4-Bank Memory Hex View
    [ObservableProperty]
    private string _bank00ReservedHex = "00000000 00000000 (Kill / Access PWD)";

    [ObservableProperty]
    private string _bank01EpcHex = string.Empty;

    [ObservableProperty]
    private string _bank10TidHex = "E280 1130 2000 0001 0203 0405 (Impinj Monza R6)";

    [ObservableProperty]
    private string _bank11UserHex = "0000 0000 0000 0000 (512-bit User Data)";

    public InspectorViewModel()
    {
        CalculateTag();
    }

    partial void OnFilterValueChanged(int value) => CalculateTag();
    partial void OnPartitionChanged(int value) => CalculateTag();
    partial void OnCompanyPrefixChanged(ulong value) => CalculateTag();
    partial void OnItemReferenceChanged(ulong value) => CalculateTag();
    partial void OnSerialNumberChanged(ulong value) => CalculateTag();

    [RelayCommand]
    public void CalculateTag()
    {
        try
        {
            CalculatedPcWord = EpcGen2Encoder.CalculatePcWord(96);
            CalculatedEpcHex = EpcGen2Encoder.EncodeSgtin96(
                FilterValue,
                Partition,
                CompanyPrefix,
                ItemReference,
                SerialNumber);

            CalculatedCrc16 = EpcGen2Encoder.CalculateEpcCrc16(CalculatedPcWord, CalculatedEpcHex);
            Bank01EpcHex = $"{CalculatedCrc16} {CalculatedPcWord} {CalculatedEpcHex}";
        }
        catch (Exception ex)
        {
            CalculatedEpcHex = $"Error: {ex.Message}";
        }
    }
}

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class DeviceUnderTestInfo : ObservableObject
{
    public event Action? DeviceUnderTestInfoChanged;
    
    [ObservableProperty]
    private string? _deviceName;
    
    partial void OnDeviceNameChanged(string? value) => DeviceUnderTestInfoChanged?.Invoke();
    
    [ObservableProperty]
    private string? _revision;
    
    partial void OnRevisionChanged(string? value) => DeviceUnderTestInfoChanged?.Invoke();
    
    [ObservableProperty]
    private string? _variant;
    
    partial void OnVariantChanged(string? value) => DeviceUnderTestInfoChanged?.Invoke();
    
    [ObservableProperty]
    private string? _partNumber;
    
    partial void OnPartNumberChanged(string? value) => DeviceUnderTestInfoChanged?.Invoke();
    
    [ObservableProperty]
    private string? _additionalNotes;
    
    partial void OnAdditionalNotesChanged(string? value) => DeviceUnderTestInfoChanged?.Invoke();
    
    public void CopyFrom(DeviceUnderTestInfo other)
    {
        DeviceName = other.DeviceName;
        Revision = other.Revision;
        Variant = other.Variant;
        PartNumber = other.PartNumber;
        AdditionalNotes = other.AdditionalNotes;
    }

    public void ResetToDefault()
    {
        DeviceName = string.Empty;
        Revision = string.Empty;
        Variant = string.Empty;
        PartNumber = string.Empty;
        AdditionalNotes = string.Empty;
    }
}
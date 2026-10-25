using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using ATLab.Enums;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// Probe settings and scan chain of the project. Devices are ordered from TDI to TDO.
/// </summary>
public partial class BoundaryScanConfiguration : ObservableObject
{
    public event Action? ConfigurationChanged;

    [ObservableProperty]
    private JtagProbeType _probeType = JtagProbeType.JLINK;

    [ObservableProperty]
    private string _probeSerialNumber = string.Empty;

    [ObservableProperty]
    private int _frequencyKHz = 1000;

    [ObservableProperty]
    private byte _ftdiLowByteValue = 0x08;

    [ObservableProperty]
    private byte _ftdiLowByteDirection = 0x0B;

    public ObservableCollection<BoundaryScanDeviceConfig> Devices { get; set; } = new();

    public BoundaryScanConfiguration()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(Devices))
                OnConfigurationChanged();
        };

        Devices.CollectionChanged += DevicesChanged;
    }

    public void ResetToDefault()
    {
        CopyFrom(new BoundaryScanConfiguration());
    }

    public BoundaryScanConfiguration Clone()
    {
        var clone = new BoundaryScanConfiguration();
        clone.CopyFrom(this);
        return clone;
    }

    public void CopyFrom(BoundaryScanConfiguration other)
    {
        ProbeType = other.ProbeType;
        ProbeSerialNumber = other.ProbeSerialNumber;
        FrequencyKHz = other.FrequencyKHz;
        FtdiLowByteValue = other.FtdiLowByteValue;
        FtdiLowByteDirection = other.FtdiLowByteDirection;

        Devices.Clear();
        foreach (var device in other.Devices)
            Devices.Add(device.Clone());
    }

    public void PrepareForSave(PathService pathService)
    {
        foreach (var device in Devices)
            device.BsdlFilePath = pathService.ToRelative(device.BsdlFilePath);
    }

    public void RestoreAfterLoad(PathService pathService)
    {
        foreach (var device in Devices)
            device.BsdlFilePath = pathService.ToAbsolute(device.BsdlFilePath);
    }

    private void DevicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (BoundaryScanDeviceConfig device in e.OldItems)
                device.PropertyChanged -= DeviceChanged;
        }

        if (e.NewItems != null)
        {
            foreach (BoundaryScanDeviceConfig device in e.NewItems)
                device.PropertyChanged += DeviceChanged;
        }

        OnConfigurationChanged();
    }

    private void DeviceChanged(object? sender, PropertyChangedEventArgs e) => OnConfigurationChanged();

    private void OnConfigurationChanged() => ConfigurationChanged?.Invoke();
}
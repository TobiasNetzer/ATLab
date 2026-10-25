using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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

    /// <summary>Buses emulated through boundary scan pins, used by "Boundary Scan Bus" test steps.</summary>
    public ObservableCollection<BoundaryScanBus> Buses { get; set; } = new();

    public BoundaryScanConfiguration()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not (nameof(Devices) or nameof(Buses)))
                OnConfigurationChanged();
        };

        Devices.CollectionChanged += DevicesChanged;
        Buses.CollectionChanged += BusesChanged;
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

        Buses.Clear();
        foreach (var bus in other.Buses)
            Buses.Add(bus.Clone());
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

    private void BusesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var bus in e.OldItems?.OfType<BoundaryScanBus>() ?? [])
            bus.PropertyChanged -= BusChanged;

        foreach (var bus in e.NewItems?.OfType<BoundaryScanBus>() ?? [])
            bus.PropertyChanged += BusChanged;

        OnConfigurationChanged();
    }

    private void BusChanged(object? sender, PropertyChangedEventArgs e) => OnConfigurationChanged();

    private void OnConfigurationChanged() => ConfigurationChanged?.Invoke();
}
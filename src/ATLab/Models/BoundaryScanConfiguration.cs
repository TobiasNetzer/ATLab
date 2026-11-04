using System;
using System.Collections.Generic;
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

    /// <summary>Nets of the interconnect test that are not in their safe state, used by "Boundary Scan Interconnect Test" steps.</summary>
    public ObservableCollection<BoundaryScanInterconnectNet> InterconnectNets { get; set; } = new();

    public BoundaryScanConfiguration()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not (nameof(Devices) or nameof(Buses) or nameof(InterconnectNets)))
                OnConfigurationChanged();
        };

        Devices.CollectionChanged += DevicesChanged;
        Buses.CollectionChanged += BusesChanged;
        InterconnectNets.CollectionChanged += InterconnectNetsChanged;
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

        InterconnectNets.Clear();
        foreach (var net in other.InterconnectNets)
            InterconnectNets.Add(net.Clone());
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

    private void InterconnectNetsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var net in e.OldItems?.OfType<BoundaryScanInterconnectNet>() ?? [])
            net.PropertyChanged -= InterconnectNetChanged;

        foreach (var net in e.NewItems?.OfType<BoundaryScanInterconnectNet>() ?? [])
            net.PropertyChanged += InterconnectNetChanged;

        OnConfigurationChanged();
    }

    private void InterconnectNetChanged(object? sender, PropertyChangedEventArgs e) => OnConfigurationChanged();

    /// <summary>The stored entry of a net; null for a net that is Safe and not connected to another.</summary>
    public BoundaryScanInterconnectNet? FindInterconnectNet(string net) =>
        InterconnectNets.FirstOrDefault(n => n.Net == net);

    public InterconnectNetRole InterconnectRoleOf(string net) => FindInterconnectNet(net)?.Role ?? InterconnectNetRole.SAFE;

    /// <summary>
    /// The net and every net connected with it, directly or through others (A to B and B to C makes A, B and C),
    /// sorted by name. The interconnect test treats them as one net.
    /// </summary>
    public List<string> ConnectedNets(string net)
    {
        var group = new List<string> { net };
        for (var i = 0; i < group.Count; i++)
        {
            var current = group[i];
            var linked = InterconnectNets
                .Where(e => e.ConnectedTo.Length > 0 && (e.Net == current || e.ConnectedTo == current))
                .Select(e => e.Net == current ? e.ConnectedTo : e.Net);
            foreach (var other in linked.Where(o => !group.Contains(o)))
                group.Add(other);
        }

        return group.Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Sets the role of a net and of the nets connected with it, as they are tested as one net.</summary>
    public void SetInterconnectRole(string net, InterconnectNetRole role)
    {
        foreach (var member in ConnectedNets(net))
            SetOwnRole(member, role);
    }

    /// <summary>
    /// Connects a net to another one, e.g. through a series resistor, or removes the connection (empty). The connected
    /// nets take one role: that of the net connected to, or this net's if that one is Safe.
    /// </summary>
    public void SetConnectedTo(string net, string other)
    {
        other = other.Trim();
        if (other == net)
            other = string.Empty;

        var entry = FindInterconnectNet(net);
        if (entry == null)
        {
            if (other.Length == 0)
                return;

            entry = new BoundaryScanInterconnectNet { Net = net };
            InterconnectNets.Add(entry);
        }

        var role = other.Length > 0 && InterconnectRoleOf(other) != InterconnectNetRole.SAFE ? InterconnectRoleOf(other) : entry.Role;
        entry.ConnectedTo = other;

        if (other.Length > 0)
            SetInterconnectRole(net, role);
        else if (entry.Role == InterconnectNetRole.SAFE)
            InterconnectNets.Remove(entry);
    }

    // Safe nets are only stored while they hold a connection
    private void SetOwnRole(string net, InterconnectNetRole role)
    {
        var entry = FindInterconnectNet(net);

        if (entry == null)
        {
            if (role != InterconnectNetRole.SAFE)
                InterconnectNets.Add(new BoundaryScanInterconnectNet { Net = net, Role = role });
            return;
        }

        if (role == InterconnectNetRole.SAFE && entry.ConnectedTo.Length == 0)
            InterconnectNets.Remove(entry);
        else
            entry.Role = role;
    }

    private void OnConfigurationChanged() => ConfigurationChanged?.Invoke();
}
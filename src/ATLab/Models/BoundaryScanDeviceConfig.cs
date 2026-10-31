using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// One device of the boundary scan chain as stored in the project.
/// </summary>
public partial class BoundaryScanDeviceConfig : ObservableObject
{
    public BoundaryScanDeviceConfig()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    // Projects saved before devices had an id get a new one
    [JsonConstructor]
    public BoundaryScanDeviceConfig(string? id)
    {
        Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
    }

    /// <summary>Referenced by test steps, stays the same when the device is renamed or moved in the chain.</summary>
    public string Id { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _bsdlFilePath = string.Empty;

    /// <summary>IDCODE found when the chain was scanned, as 8 hex digits. Empty for devices without IDCODE.</summary>
    [ObservableProperty]
    private string _idCode = string.Empty;

    /// <summary>Taken from the BSDL file, has to be entered manually for devices without one.</summary>
    [ObservableProperty]
    private int _irLength;

    [ObservableProperty]
    private string _packageName = string.Empty;

    /// <summary>
    /// User defined signal names keyed by port, e.g. "IO(3)" = "LED_RED". Keyed by port rather than package pin
    /// so the labels survive a package change. Labels of ports missing in the current BSDL are kept.
    /// </summary>
    public Dictionary<string, string> PinLabels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string GetPinLabel(string port) => PinLabels.GetValueOrDefault(port, string.Empty);

    public void SetPinLabel(string port, string label)
    {
        label = label.Trim();

        if (GetPinLabel(port) == label)
            return;

        if (label.Length == 0)
            PinLabels.Remove(port);
        else
            PinLabels[port] = label;

        OnPropertyChanged(nameof(PinLabels));
    }

    public BoundaryScanDeviceConfig Clone() => new(Id)
    {
        Name = Name,
        BsdlFilePath = BsdlFilePath,
        IdCode = IdCode,
        IrLength = IrLength,
        PackageName = PackageName,
        PinLabels = new Dictionary<string, string>(PinLabels, StringComparer.OrdinalIgnoreCase)
    };
}
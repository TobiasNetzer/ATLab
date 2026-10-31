using System.Text.Json.Serialization;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>Assigns a boundary scan pin (device id and port) to a signal of a bus.</summary>
public partial class BoundaryScanBusPin : ObservableObject
{
    [ObservableProperty]
    private BoundaryScanBusSignal _signal;

    /// <summary>Bit number of a pin group, 0 for all other signals.</summary>
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAssigned))]
    private string _deviceId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAssigned))]
    private string _port = string.Empty;

    [JsonIgnore]
    public bool IsAssigned => DeviceId.Length > 0 && Port.Length > 0;

    [JsonIgnore]
    public string SignalName => Signal == BoundaryScanBusSignal.BIT ? $"Bit {Index}" : Signal.ToString();

    public BoundaryScanBusPin Clone() => new()
    {
        Signal = Signal,
        Index = Index,
        DeviceId = DeviceId,
        Port = Port
    };
}
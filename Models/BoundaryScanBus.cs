using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// A serial bus or pin group whose signals are driven and read through boundary scan pins.
/// Defined in the Boundary Scan tab and referenced by id from test steps.
/// </summary>
public partial class BoundaryScanBus : ObservableObject
{
    public const int MaxPinGroupWidth = 32;

    public BoundaryScanBus()
    {
        Id = Guid.NewGuid().ToString("N");
        SubscribePins(Pins);
    }

    [JsonConstructor]
    public BoundaryScanBus(string? id) : this()
    {
        if (!string.IsNullOrWhiteSpace(id))
            Id = id;
    }

    public string Id { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private BoundaryScanBusType _type;

    /// <summary>SPI mode 0-3, bit 1 is CPOL, bit 0 is CPHA.</summary>
    [ObservableProperty]
    private int _spiMode;

    [ObservableProperty]
    private bool _isCsActiveHigh;

    [ObservableProperty]
    private bool _isLsbFirst;

    /// <summary>Number of pins of a pin group.</summary>
    [ObservableProperty]
    private int _width = 8;

    [ObservableProperty]
    private ObservableCollection<BoundaryScanBusPin> _pins = new();

    public BoundaryScanBusPin? GetPin(BoundaryScanBusSignal signal, int index = 0) =>
        Pins.FirstOrDefault(p => p.Signal == signal && p.Index == index);

    /// <summary>
    /// Brings the pin list in line with the bus type and width. Assignments of signals that remain are kept.
    /// </summary>
    public void UpdateSignals()
    {
        List<(BoundaryScanBusSignal Signal, int Index)> required = Type switch
        {
            BoundaryScanBusType.I2C => [(BoundaryScanBusSignal.SCL, 0), (BoundaryScanBusSignal.SDA, 0)],
            BoundaryScanBusType.SPI =>
            [
                (BoundaryScanBusSignal.SCK, 0), (BoundaryScanBusSignal.MOSI, 0),
                (BoundaryScanBusSignal.MISO, 0), (BoundaryScanBusSignal.CS, 0)
            ],
            _ => Enumerable.Range(0, Math.Clamp(Width, 1, MaxPinGroupWidth))
                .Select(i => (BoundaryScanBusSignal.BIT, i))
                .ToList()
        };

        var updated = required
            .Select(r => GetPin(r.Signal, r.Index) ?? new BoundaryScanBusPin { Signal = r.Signal, Index = r.Index })
            .ToList();

        if (updated.SequenceEqual(Pins))
            return;

        Pins = new ObservableCollection<BoundaryScanBusPin>(updated);
    }

    public BoundaryScanBus Clone() => new(Id)
    {
        Name = Name,
        Type = Type,
        SpiMode = SpiMode,
        IsCsActiveHigh = IsCsActiveHigh,
        IsLsbFirst = IsLsbFirst,
        Width = Width,
        Pins = new ObservableCollection<BoundaryScanBusPin>(Pins.Select(p => p.Clone()))
    };

    partial void OnPinsChanged(ObservableCollection<BoundaryScanBusPin>? oldValue, ObservableCollection<BoundaryScanBusPin> newValue)
    {
        if (oldValue != null)
        {
            oldValue.CollectionChanged -= PinsCollectionChanged;
            foreach (var pin in oldValue)
                pin.PropertyChanged -= PinChanged;
        }

        SubscribePins(newValue);
    }

    private void SubscribePins(ObservableCollection<BoundaryScanBusPin> pins)
    {
        pins.CollectionChanged += PinsCollectionChanged;
        foreach (var pin in pins)
            pin.PropertyChanged += PinChanged;
    }

    private void PinsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var pin in e.OldItems?.OfType<BoundaryScanBusPin>() ?? [])
            pin.PropertyChanged -= PinChanged;

        foreach (var pin in e.NewItems?.OfType<BoundaryScanBusPin>() ?? [])
            pin.PropertyChanged += PinChanged;

        OnPropertyChanged(nameof(Pins));
    }

    // Lets the configuration report the change, which marks the project dirty. Computed flags are skipped.
    private void PinChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BoundaryScanBusPin.DeviceId) or nameof(BoundaryScanBusPin.Port))
            OnPropertyChanged(nameof(Pins));
    }
}
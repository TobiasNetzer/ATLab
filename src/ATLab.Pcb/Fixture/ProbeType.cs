using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Pcb.Fixture;

/// <summary>
/// A kind of spring probe with the dimensions the checks and the plate files need. Defined by the user, e.g. after
/// the data sheet of the probes and receptacles in use.
/// </summary>
public partial class ProbeType : ObservableObject
{
    public string Id { get; init; } = NewId();

    [ObservableProperty] private string _name = string.Empty;

    /// <summary>Smallest pad the tip can hit reliably, in mm.</summary>
    [ObservableProperty] private double _minPadSize;

    /// <summary>Diameter of the hole in the plate for the receptacle, in mm.</summary>
    [ObservableProperty] private double _drillDiameter;

    /// <summary>Smallest distance between the centers of two probes, in mm.</summary>
    [ObservableProperty] private double _minSpacing;

    /// <summary>Smallest distance from the probe center to the courtyard of another component, in mm.</summary>
    [ObservableProperty] private double _componentClearance;

    [ObservableProperty] private string _note = string.Empty;

    public static string NewId() => Guid.NewGuid().ToString("N")[..12];

    public ProbeType Copy(bool newId = false) => new()
    {
        Id = newId ? NewId() : Id,
        Name = Name,
        MinPadSize = MinPadSize,
        DrillDiameter = DrillDiameter,
        MinSpacing = MinSpacing,
        ComponentClearance = ComponentClearance,
        Note = Note
    };

    public override string ToString() => Name;
}
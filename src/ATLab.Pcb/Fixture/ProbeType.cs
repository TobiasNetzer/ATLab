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

    /// <summary>Diameter of the probe, in mm.</summary>
    [ObservableProperty] private double _diameter;

    /// <summary>Diameter of the hole in the plate for the receptacle, in mm.</summary>
    [ObservableProperty] private double _drillDiameter;

    /// <summary>
    /// Diameter of the opening in the moving plate the probe comes through, in mm; 0 for the drill diameter. A probe
    /// can have an opening of its own.
    /// </summary>
    [ObservableProperty] private double _openingDiameter;

    /// <summary>How far the probe stands out of the plate towards the board, in mm.</summary>
    [ObservableProperty] private double _height;

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
        Diameter = Diameter,
        DrillDiameter = DrillDiameter,
        OpeningDiameter = OpeningDiameter,
        Height = Height,
        MinSpacing = MinSpacing,
        ComponentClearance = ComponentClearance,
        Note = Note
    };

    public override string ToString() => Name;
}
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Pcb.Fixture;

/// <summary>Mechanical parts of a bed-of-nails fixture besides the probes.</summary>
public enum FixturePartKind
{
    /// <summary>A pin in the bottom plate that stands in a hole of the board, plated or not, and positions it.</summary>
    LocatingPin,

    /// <summary>
    /// A pin in the bottom plate at the board edge, which centers the board roughly when it is put in; the locating
    /// pins in its holes then position it exactly.
    /// </summary>
    PreCenteringPin,

    /// <summary>A pin in the bottom plate the board rests on.</summary>
    SupportPin,

    /// <summary>A rod in the top plate that presses the board down onto the probes.</summary>
    PushRod
}

/// <summary>
/// A kind of locating pin, pre-centering pin, support pin or push rod with the dimensions the checks, the plate files and the assembly
/// need. Defined by the user, like the probe types.
/// </summary>
public partial class PartType : ObservableObject
{
    public string Id { get; init; } = ProbeType.NewId();

    [ObservableProperty] private FixturePartKind _kind;

    [ObservableProperty] private string _name = string.Empty;

    /// <summary>Diameter of the pin or rod where it touches the board, in mm.</summary>
    [ObservableProperty] private double _diameter;

    /// <summary>Diameter of the hole in the plate, in mm.</summary>
    [ObservableProperty] private double _drillDiameter;

    /// <summary>How far it stands out of the plate towards the board, in mm.</summary>
    [ObservableProperty] private double _height;

    /// <summary>Smallest distance from its edge to the courtyard of a component on its side of the board, in mm.</summary>
    [ObservableProperty] private double _componentClearance;

    [ObservableProperty] private string _note = string.Empty;

    public PartType Copy(bool newId = false) => new()
    {
        Id = newId ? ProbeType.NewId() : Id,
        Kind = Kind,
        Name = Name,
        Diameter = Diameter,
        DrillDiameter = DrillDiameter,
        Height = Height,
        ComponentClearance = ComponentClearance,
        Note = Note
    };

    public override string ToString() => Name;
}

/// <summary>A placed locating pin, pre-centering pin, support pin or push rod: its kind, type and position on the board.</summary>
public sealed class FixturePartEntry
{
    public int Number { get; set; }
    public FixturePartKind Kind { get; set; }

    /// <summary>ID of its type.</summary>
    public string? Type { get; set; }

    /// <summary>Position in board coordinates (KiCad, mm).</summary>
    public double X { get; set; }
    public double Y { get; set; }
}
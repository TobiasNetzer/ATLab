using System;
using ATLab.Pcb.Kicad;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Pcb.Fixture;

/// <summary>Mechanical parts of a bed-of-nails fixture besides the probes.</summary>
public enum FixturePartKind
{
    /// <summary>A pin in the moving plate that stands in a hole of the board, plated or not, and positions it.</summary>
    LocatingPin,

    /// <summary>
    /// A pin in the moving plate at the board edge, which centers the board roughly when it is put in; the locating
    /// pins in its holes then position it exactly.
    /// </summary>
    PreCenteringPin,

    /// <summary>A pin in the moving plate the board rests on.</summary>
    SupportPin,

    /// <summary>A rod in the pressure plate that presses the board down onto the probes.</summary>
    PushRod
}

/// <summary>
/// A kind of locating pin, pre-centering pin, support pin or push rod with the dimensions the checks, the plate files and the assembly
/// need. Defined by the user, like the probe types.
/// </summary>
public partial class PartType : ObservableObject
{
    public string Id { get; init; } = ProbeType.NewId();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPreCenteringPin))]
    private FixturePartKind _kind;

    public bool IsPreCenteringPin => Kind == FixturePartKind.PreCenteringPin;

    [ObservableProperty] private string _name = string.Empty;

    /// <summary>Diameter of the pin or rod where it touches the board, in mm.</summary>
    [ObservableProperty] private double _diameter;

    /// <summary>Diameter of the hole in the plate, in mm.</summary>
    [ObservableProperty] private double _drillDiameter;

    /// <summary>
    /// Push rods: diameter of the opening in the top probe plate the rod comes through, in mm; 0 for the drill
    /// diameter. A push rod can have an opening of its own.
    /// </summary>
    [ObservableProperty] private double _openingDiameter;

    /// <summary>Pre-centering pins: gap between the pin and the board edge, so the board drops in freely, in mm.</summary>
    [ObservableProperty] private double _edgeGap;

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
        OpeningDiameter = OpeningDiameter,
        EdgeGap = EdgeGap,
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

    /// <summary>Push rods: Ø of its opening in the top probe plate if not the type's, in mm.</summary>
    public double? Opening { get; set; }
}
/// <summary>
/// The plates of a bed-of-nails fixture, from the bottom up: the probe plate with the bottom probes, the moving plate
/// the board lies on (locating, pre-centering and support pins, openings for the bottom probes), the probe plate with
/// the top probes (openings for the push rods), only when there are top probes, and the pressure plate with the push
/// rods.
/// </summary>
public enum FixturePlate
{
    BottomProbePlate,
    MovingPlate,
    TopProbePlate,
    PressurePlate
}

public static class FixturePlates
{
    /// <summary>The plates below the board: the bottom probe plate and the moving plate.</summary>
    public static bool IsBelowBoard(this FixturePlate plate) =>
        plate is FixturePlate.BottomProbePlate or FixturePlate.MovingPlate;

    /// <summary>
    /// The side of the board the plate faces: the plates below the board face its bottom side, or its top side when
    /// the board lies upside down in the fixture.
    /// </summary>
    public static BoardSide Side(this FixturePlate plate, bool isBoardFlipped) =>
        plate.IsBelowBoard() != isBoardFlipped ? BoardSide.Bottom : BoardSide.Top;

    /// <summary>The side of the board that faces down in the fixture, towards the moving plate.</summary>
    public static BoardSide DownSide(bool isBoardFlipped) => isBoardFlipped ? BoardSide.Top : BoardSide.Bottom;

    /// <summary>The plate that holds the probes of a side of the board.</summary>
    public static FixturePlate ProbePlate(BoardSide side, bool isBoardFlipped) =>
        side == DownSide(isBoardFlipped) ? FixturePlate.BottomProbePlate : FixturePlate.TopProbePlate;
}

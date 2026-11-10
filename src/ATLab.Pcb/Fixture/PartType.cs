using System;
using ATLab.Pcb.Kicad;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Pcb.Fixture;

/// <summary>Mechanical parts of a bed-of-nails fixture besides the probes.</summary>
public enum FixturePartKind
{
    /// <summary>A pin in the moving plate that stands in a hole of the board, plated or not, and positions it.</summary>
    ToolingPin,

    /// <summary>
    /// A pin in the moving plate at the board edge, which centers the board roughly when it is put in; the tooling
    /// pins in its holes then position it exactly.
    /// </summary>
    PreCenteringPin,

    /// <summary>A pin in the moving plate the board rests on.</summary>
    SupportPin,

    /// <summary>A rod in the pressure plate that presses the board down onto the probes.</summary>
    PushRod
}

/// <summary>The tip of a pin or rod.</summary>
public enum PartTipStyle
{
    Flat,
    Chamfer,

    /// <summary>A half sphere of the tip's diameter.</summary>
    Round
}

/// <summary>
/// A kind of tooling pin, pre-centering pin, support pin or push rod with the dimensions the checks, the plate files and the assembly
/// need. Defined by the user, like the probe types.
/// </summary>
public partial class PartType : ObservableObject
{
    public string Id { get; init; } = ProbeType.NewId();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPreCenteringPin), nameof(IsPushRod), nameof(IsToolingPin), nameof(CanHaveBody), nameof(HasBody), nameof(HasStepChamfer))]
    private FixturePartKind _kind;

    public bool IsPreCenteringPin => Kind == FixturePartKind.PreCenteringPin;
    public bool IsPushRod => Kind == FixturePartKind.PushRod;
    public bool IsToolingPin => Kind == FixturePartKind.ToolingPin;

    /// <summary>Push rods can have a thicker body, tooling pins a shoulder the board sits on.</summary>
    public bool CanHaveBody => IsPushRod || IsToolingPin;

    /// <summary>Only a push rod's step gets a chamfer: the board has to lie flat on the shoulder of a tooling pin.</summary>
    public bool HasStepChamfer => IsPushRod && HasBody;

    /// <summary>The part is thicker below its thin end.</summary>
    public bool HasBody => CanHaveBody && BodyDiameter > 0;

    [ObservableProperty] private string _name = string.Empty;

    /// <summary>Diameter of the pin or rod where it touches the board, in mm.</summary>
    [ObservableProperty] private double _diameter;

    /// <summary>Diameter of the hole in the plate, in mm.</summary>
    [ObservableProperty] private double _drillDiameter;

    /// <summary>
    /// Push rods: diameter of the opening in the top probe plate the rod comes through, in mm; 0 for the drill
    /// diameter.
    /// </summary>
    [ObservableProperty] private double _openingDiameter;

    /// <summary>Pre-centering pins: gap between the pin and the board edge, so the board drops in freely, in mm.</summary>
    [ObservableProperty] private double _edgeGap;

    /// <summary>How far it stands out of the plate towards the board in total, in mm.</summary>
    [ObservableProperty] private double _height;

    /// <summary>Push rods: Ø of the body behind the thin end; tooling pins: Ø of the shoulder the board sits on; 0 for none, in mm.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBody), nameof(HasStepChamfer))]
    private double _bodyDiameter;

    /// <summary>Length of the thin end, from the tip to the body or shoulder, in mm.</summary>
    [ObservableProperty] private double _thinLength;

    /// <summary>Push rods: 45° chamfer in the corner between thin end and body, which keeps a printed rod from breaking there, in mm.</summary>
    [ObservableProperty] private double _stepChamfer = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTipChamfer))]
    private PartTipStyle _tipStyle;

    public bool HasTipChamfer => TipStyle == PartTipStyle.Chamfer;

    /// <summary>Size of the 45° chamfer at the tip, in mm.</summary>
    [ObservableProperty] private double _tipSize = 0.5;

    /// <summary>The 3D print export models the part onto its plate instead of drilling a hole for it.</summary>
    [ObservableProperty] private bool _isPrinted;

    /// <summary>Smallest distance from its edge to the courtyard of a component on its side of the board, in mm.</summary>
    [ObservableProperty] private double _componentClearance;

    /// <summary>Manufacturer of the part, for the bill of materials.</summary>
    [ObservableProperty] private string _manufacturer = string.Empty;

    /// <summary>Part number of the part, for the bill of materials.</summary>
    [ObservableProperty] private string _partNumber = string.Empty;

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
        BodyDiameter = BodyDiameter,
        ThinLength = ThinLength,
        StepChamfer = StepChamfer,
        TipStyle = TipStyle,
        TipSize = TipSize,
        IsPrinted = IsPrinted,
        ComponentClearance = ComponentClearance,
        Manufacturer = Manufacturer,
        PartNumber = PartNumber,
        Note = Note
    };

    public override string ToString() => Name;
}

/// <summary>A placed tooling pin, pre-centering pin, support pin or push rod: its kind, type and position on the board.</summary>
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
/// <summary>
/// The plates of a bed-of-nails fixture, from the bottom up: the probe plate with the bottom probes, the moving plate
/// the board lies on (tooling, pre-centering and support pins, openings for the bottom probes), the probe plate with
/// the top probes (openings for the push rods), only when there are top probes, and the pressure plate with the push
/// rods. A fixture with one combined top plate has no top probe plate: its pressure plate holds the top probes too.
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

    /// <summary>
    /// The plate that holds the probes of a side of the board: above the board the top probe plate, or the pressure
    /// plate when it holds the top probes and the push rods together.
    /// </summary>
    public static FixturePlate ProbePlate(BoardSide side, bool isBoardFlipped, bool isTopPlateCombined = false) =>
        side == DownSide(isBoardFlipped) ? FixturePlate.BottomProbePlate
        : isTopPlateCombined ? FixturePlate.PressurePlate
        : FixturePlate.TopProbePlate;
}

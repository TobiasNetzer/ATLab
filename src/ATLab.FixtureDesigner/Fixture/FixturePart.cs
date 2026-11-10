using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ATLab.FixtureDesigner.Localization;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.FixtureDesigner.Fixture;

/// <summary>
/// A tooling pin, pre-centering pin, support pin or push rod placed by the user. Its position is kept in board coordinates; X and Y are
/// the same point in build sheet coordinates, which the user can type.
/// </summary>
public partial class FixturePart : ObservableObject, IMovableBoardMarker
{
    public required FixturePartKind Kind { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name))]
    private int _number;

    /// <summary>T1 for tooling pins, C1 for pre-centering pins, S1 for support pins, R1 for push rods.</summary>
    public string Name => Prefix(Kind) + Number;

    public static string Prefix(FixturePartKind kind) => kind switch
    {
        FixturePartKind.ToolingPin => "T",
        FixturePartKind.PreCenteringPin => "C",
        FixturePartKind.SupportPin => "S",
        _ => "R"
    };

    public string KindLabel => Localizer.Get(KindKey(Kind));

    public static string KindKey(FixturePartKind kind) => kind switch
    {
        FixturePartKind.ToolingPin => "Part_ToolingPin",
        FixturePartKind.PreCenteringPin => "Part_PreCenteringPin",
        FixturePartKind.SupportPin => "Part_SupportPin",
        _ => "Part_PushRod"
    };

    [ObservableProperty] private PartType? _type;

    /// <summary>Types of the fixture of this part's kind, for the type selection in the list.</summary>
    [ObservableProperty] private IReadOnlyList<PartType> _availableTypes = [];

    /// <summary>Position on the board, KiCad coordinates in mm.</summary>
    [ObservableProperty] private double _boardX;

    [ObservableProperty] private double _boardY;

    /// <summary>The same position in build sheet coordinates (Y up), as the user types it.</summary>
    [ObservableProperty] private double _x;

    [ObservableProperty] private double _y;

    /// <summary>Worst result of the checks for this part.</summary>
    [ObservableProperty] private BoardMarkerState _state;

    /// <summary>Plate the part goes into: tooling, pre-centering and support pins the moving plate, push rods the pressure plate.</summary>
    public FixturePlate Plate => Kind == FixturePartKind.PushRod ? FixturePlate.PressurePlate : FixturePlate.MovingPlate;

    /// <summary>The board lies upside down in the fixture: the pins are at its top side, the push rods at its bottom side.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlateSide))]
    private bool _isBoardFlipped;

    /// <summary>The side of the board the part is on.</summary>
    public BoardSide PlateSide => Plate.Side(IsBoardFlipped);

    /// <summary>The last hole a tooling pin stood in, where it goes back to when dropped beside any hole.</summary>
    public Point2? Anchor { get; set; }

    public void MoveTo(Point2 boardPoint)
    {
        BoardX = boardPoint.X;
        BoardY = boardPoint.Y;
    }

    partial void OnTypeChanged(PartType? value)
    {
        OnPropertyChanged(nameof(IBoardMarker.MarkerSize));
        RefreshOpening();
    }

    /// <summary>Push rods come through an opening of the top probe plate; the pins have none.</summary>
    public bool HasOpening => Kind == FixturePartKind.PushRod;

    /// <summary>Ø of the opening in the top probe plate: the type's, else the type's drill Ø; 0 for none.</summary>
    public double Opening => !HasOpening ? 0 : TypeOpening(Type);

    public static double TypeOpening(PartType? type) =>
        type is null ? 0 : type.OpeningDiameter > 0 ? type.OpeningDiameter : type.DrillDiameter;

    /// <summary>The type's sizes changed: the opening may follow them.</summary>
    public void RefreshOpening() => OnPropertyChanged(nameof(Opening));

    // ----- Drawing on the board -----

    BoardMarkerKind IBoardMarker.MarkerKind => Kind switch
    {
        FixturePartKind.ToolingPin => BoardMarkerKind.ToolingPin,
        FixturePartKind.PreCenteringPin => BoardMarkerKind.PreCenteringPin,
        FixturePartKind.SupportPin => BoardMarkerKind.SupportPin,
        _ => BoardMarkerKind.PushRod
    };

    Point2? IBoardMarker.MarkerPosition => new Point2(BoardX, BoardY);
    double IBoardMarker.MarkerSize => Type?.Diameter is > 0 and var d ? d : 2;

    // A pre-centering pin stands beside the board, a tooling pin goes through it: both show from both sides
    BoardSide? IBoardMarker.MarkerSide => Kind is FixturePartKind.PreCenteringPin or FixturePartKind.ToolingPin ? null : PlateSide;
    string IBoardMarker.MarkerLabel => Name;
    BoardMarkerState IBoardMarker.MarkerState => State;
    Pad? IBoardMarker.MarkerPad => null;
}

/// <summary>
/// The user's types of tooling, pre-centering and support pins and push rods, saved as part-library.json next to
/// ATLab's settings, like the probe library: a new fixture starts with a copy, every fixture keeps its own.
/// </summary>
public static class PartLibrary
{
    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ATLab", "part-library.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// The saved library, or example types if none is saved yet. A kind the library has no type of gets its example,
    /// e.g. a library saved before the user had types of every kind.
    /// </summary>
    public static List<PartType> Load()
    {
        List<PartType>? types = null;
        try
        {
            if (File.Exists(FilePath))
                types = JsonSerializer.Deserialize<List<PartType>>(File.ReadAllText(FilePath), JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // An unreadable library is replaced by the examples; the file stays as it is until the user saves
        }

        if (types is not { Count: > 0 })
            return Examples();

        types.AddRange(Examples().Where(example => types.All(t => t.Kind != example.Kind)));
        return types;
    }

    public static void Save(IEnumerable<PartType> types)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(types.ToList(), JsonOptions));
        File.Move(temp, FilePath, overwrite: true);
    }

    /// <summary>Adds or replaces a type (same ID) in the saved library.</summary>
    public static void Store(PartType type)
    {
        var library = Load();
        library.RemoveAll(t => t.Id == type.Id);
        library.Add(type.Copy());
        Save(library);
    }

    /// <summary>Usual sizes as a start; marked as examples to replace with the parts in use.</summary>
    public static List<PartType> Examples() =>
    [
        new() { Id = "exampleToolingPin", Kind = FixturePartKind.ToolingPin, Name = "Tooling pin 4 mm", Diameter = 4, DrillDiameter = 2, Height = 6, ComponentClearance = 0},
        new() { Id = "examplePreCenteringPin", Kind = FixturePartKind.PreCenteringPin, Name = "Pre-centering pin 12 mm", Diameter = 12, DrillDiameter = 4, EdgeGap = 0.3, Height = 22, ComponentClearance = 0.5},
        new() { Id = "exampleSupport", Kind = FixturePartKind.SupportPin, Name = "Support pin 5 mm", Diameter = 5, DrillDiameter = 3, Height = 2.8, ComponentClearance = 1},
        new() { Id = "examplePushRod", Kind = FixturePartKind.PushRod, Name = "Push rod 2.5 mm", Diameter = 2.5, DrillDiameter = 4, OpeningDiameter = 5, Height = 50, ComponentClearance = 1}
    ];
}
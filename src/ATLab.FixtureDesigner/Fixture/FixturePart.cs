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
/// A locating pin, pre-centering pin, support pin or push rod placed by the user. Its position is kept in board coordinates; X and Y are
/// the same point in build sheet coordinates, which the user can type.
/// </summary>
public partial class FixturePart : ObservableObject, IMovableBoardMarker
{
    public required FixturePartKind Kind { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name))]
    private int _number;

    /// <summary>L1 for locating pins, C1 for pre-centering pins, S1 for support pins, R1 for push rods.</summary>
    public string Name => Prefix(Kind) + Number;

    public static string Prefix(FixturePartKind kind) => kind switch
    {
        FixturePartKind.LocatingPin => "L",
        FixturePartKind.PreCenteringPin => "C",
        FixturePartKind.SupportPin => "S",
        _ => "R"
    };

    public string KindLabel => Localizer.Get(KindKey(Kind));

    public static string KindKey(FixturePartKind kind) => kind switch
    {
        FixturePartKind.LocatingPin => "Part_LocatingPin",
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

    /// <summary>Plate the part goes into: locating, pre-centering and support pins the bottom one, push rods the top one.</summary>
    public BoardSide PlateSide => Kind == FixturePartKind.PushRod ? BoardSide.Top : BoardSide.Bottom;

    /// <summary>The last hole a locating pin stood in, where it goes back to when dropped beside any hole.</summary>
    public Point2? Anchor { get; set; }

    public void MoveTo(Point2 boardPoint)
    {
        BoardX = boardPoint.X;
        BoardY = boardPoint.Y;
    }

    partial void OnTypeChanged(PartType? value) => OnPropertyChanged(nameof(IBoardMarker.MarkerSize));

    // ----- Drawing on the board -----

    BoardMarkerKind IBoardMarker.MarkerKind => Kind switch
    {
        FixturePartKind.LocatingPin => BoardMarkerKind.LocatingPin,
        FixturePartKind.PreCenteringPin => BoardMarkerKind.PreCenteringPin,
        FixturePartKind.SupportPin => BoardMarkerKind.SupportPin,
        _ => BoardMarkerKind.PushRod
    };

    Point2? IBoardMarker.MarkerPosition => new Point2(BoardX, BoardY);
    double IBoardMarker.MarkerSize => Type?.Diameter is > 0 and var d ? d : 2;

    // A pre-centering pin stands beside the board, a locating pin goes through it: both show from both sides
    BoardSide? IBoardMarker.MarkerSide => Kind is FixturePartKind.PreCenteringPin or FixturePartKind.LocatingPin ? null : PlateSide;
    string IBoardMarker.MarkerLabel => Name;
    BoardMarkerState IBoardMarker.MarkerState => State;
    Pad? IBoardMarker.MarkerPad => null;
}

/// <summary>
/// The user's types of locating, pre-centering and support pins and push rods, saved as part-library.json next to
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

    public const string ExampleNote = "Example values, check the data sheet of your pins and rods";

    /// <summary>Usual sizes as a start; marked as examples to replace with the parts in use.</summary>
    public static List<PartType> Examples() =>
    [
        new() { Id = "exampleLocatingPin3", Kind = FixturePartKind.LocatingPin, Name = "Locating pin 3 mm", Diameter = 3, DrillDiameter = 3, Length = 25, Height = 12, ComponentClearance = 0, Note = ExampleNote },
        new() { Id = "exampleLocating3", Kind = FixturePartKind.PreCenteringPin, Name = "Pre-centering pin 3 mm", Diameter = 3, DrillDiameter = 3, Length = 20, Height = 8, ComponentClearance = 0.5, Note = ExampleNote },
        new() { Id = "exampleSupport4", Kind = FixturePartKind.SupportPin, Name = "Support pin 4 mm", Diameter = 4, DrillDiameter = 4, Length = 25, Height = 10, ComponentClearance = 1, Note = ExampleNote },
        new() { Id = "examplePushRod5", Kind = FixturePartKind.PushRod, Name = "Push rod 5 mm", Diameter = 5, DrillDiameter = 5, Length = 40, Height = 15, ComponentClearance = 1, Note = ExampleNote }
    ];
}
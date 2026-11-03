using ATLab.Pcb.Kicad;
using CommunityToolkit.Mvvm.ComponentModel;

using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;

namespace ATLab.FixtureDesigner.Fixture;

/// <summary>A probe of the fixture while it is edited, with the pad of the loaded board it touches.</summary>
public partial class Probe : ObservableObject, IBoardMarker
{
    public Probe(string reference, string padNumber, BoardSide side)
    {
        Reference = reference;
        PadNumber = padNumber;
        Side = side;
    }

    public string Reference { get; }
    public string PadNumber { get; }
    public BoardSide Side { get; }

    public string Key => Pad.PadKeyOf(Reference, PadNumber);

    /// <summary>Probe number on the drawing and in the build sheet (P1, P2 …).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name))]
    private int _number;

    public string Name => $"P{Number}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PinDisplay))]
    private AdapterPin? _pin;

    public string PinDisplay => Pin?.Display ?? string.Empty;

    [ObservableProperty] private string _note = string.Empty;

    /// <summary>Kind of probe: its dimensions are used by the spacing checks and the plate files.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeName))]
    private ProbeType? _type;

    public string TypeName => Type?.Name ?? string.Empty;

    /// <summary>Pad on the loaded board; null if the board no longer has it.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Net), nameof(ShortNet), nameof(IsMissing))]
    private Pad? _pad;

    /// <summary>Net and position from the fixture file, to notice changes of the board.</summary>
    public string SavedNet { get; set; } = string.Empty;

    public Point2? SavedPosition { get; set; }

    public string Net => Pad?.Net ?? SavedNet;

    /// <summary>Net without the sheet path, e.g. "SDA" for "/MCU/SDA".</summary>
    public string ShortNet => ShortNetName(Net);

    public bool IsMissing => Pad is null;

    /// <summary>Position in build sheet coordinates (mm from the origin, Y up), set when the board is known.</summary>
    [ObservableProperty] private double _x;

    [ObservableProperty] private double _y;

    /// <summary>Worst result of the checks for this probe, for the colors in the list and on the board.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(HasWarning))]
    private CheckSeverity? _severity;

    public bool HasError => Severity == CheckSeverity.Error;
    public bool HasWarning => Severity == CheckSeverity.Warning;

    public string PadDisplay => $"{Reference}.{PadNumber}";

    public static string ShortNetName(string net)
    {
        var slash = net.LastIndexOf('/');
        return slash >= 0 && slash < net.Length - 1 ? net[(slash + 1)..] : net;
    }

    // ----- Drawing on the board -----

    BoardMarkerKind IBoardMarker.MarkerKind => BoardMarkerKind.Probe;
    Point2? IBoardMarker.MarkerPosition => Pad?.Position;
    double IBoardMarker.MarkerSize => Pad?.MinSize ?? 1;
    BoardSide? IBoardMarker.MarkerSide => Side;
    string IBoardMarker.MarkerLabel => Name;

    BoardMarkerState IBoardMarker.MarkerState => Severity switch
    {
        CheckSeverity.Error => BoardMarkerState.Error,
        CheckSeverity.Warning => BoardMarkerState.Warning,
        _ => BoardMarkerState.Normal
    };

    Pad? IBoardMarker.MarkerPad => Pad;
}
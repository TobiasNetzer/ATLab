using System;
using System.ComponentModel;
using System.Globalization;
using ATLab.Pcb.Kicad;
using Avalonia.Media;

namespace ATLab.Pcb.Controls;

public enum BoardMarkerKind
{
    /// <summary>A probe: filled circle with a label, dashed when it comes from the other side.</summary>
    Probe,

    /// <summary>A tooling pin: ring with a cross, through all sides.</summary>
    ToolingPin,

    /// <summary>A pre-centering pin against the board edge.</summary>
    PreCenteringPin,

    /// <summary>A support pin under the board.</summary>
    SupportPin,

    /// <summary>A push rod pressing on the board from above.</summary>
    PushRod
}

/// <summary>A marker the user can drag to another place on the board.</summary>
public interface IMovableBoardMarker : IBoardMarker
{
    /// <summary>Moves the marker to a point in board coordinates (mm).</summary>
    void MoveTo(Point2 boardPoint);
}

public enum BoardMarkerState
{
    Normal,
    Warning,
    Error
}

/// <summary>Something drawn on the board at a position, e.g. a probe of the fixture or a tooling pin.</summary>
public interface IBoardMarker : INotifyPropertyChanged
{
    BoardMarkerKind MarkerKind { get; }

    /// <summary>Board coordinates (KiCad, mm); null hides the marker.</summary>
    Point2? MarkerPosition { get; }

    /// <summary>Size of the pad or hole in mm, for the size of the marker.</summary>
    double MarkerSize { get; }

    /// <summary>Side the probe comes from; null for markers through the board.</summary>
    BoardSide? MarkerSide { get; }

    string MarkerLabel { get; }
    BoardMarkerState MarkerState { get; }

    /// <summary>Pad of the marker, for the hover information; null if none.</summary>
    Pad? MarkerPad { get; }

    /// <summary>Own color instead of the color of the state.</summary>
    Color? MarkerColor => null;
}

public enum NetHighlightStyle
{
    /// <summary>Pads, tracks, vias and zones filled in the color; the rest of the board is dimmed.</summary>
    Solid,

    /// <summary>Pads and tracks outlined dashed, e.g. to keep failed steps visible under the current one.</summary>
    Outline
}

/// <summary>A net drawn in a color: all its pads, tracks, vias and zones.</summary>
public sealed record NetHighlight(string Net, Color Color, NetHighlightStyle Style = NetHighlightStyle.Solid);

/// <summary>A single pad in a color with an optional label, e.g. a pin of a boundary scan device.</summary>
public sealed record PadHighlight(Pad Pad, Color Color, string? Label = null);

/// <summary>A component outlined in a color, e.g. the chip of a boundary scan step.</summary>
public sealed record FootprintHighlight(string Reference, Color Color);

/// <summary>
/// Texts of the board view in the language of the application. Each application sets <see cref="Lookup"/> to its
/// resources and calls <see cref="Refresh"/> after a language change.
/// </summary>
public static class BoardTexts
{
    /// <summary>Text of a key; the key itself if there is none.</summary>
    public static Func<string, string?> Lookup { get; set; } = _ => null;

    public static event EventHandler? Changed;

    public static void Refresh() => Changed?.Invoke(null, EventArgs.Empty);

    public static string Get(string key) => Lookup(key) ?? key;

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    /// <summary>Net without the sheet path, e.g. "SDA" for "/MCU/SDA".</summary>
    public static string ShortNet(string net)
    {
        var slash = net.LastIndexOf('/');
        return slash >= 0 && slash < net.Length - 1 ? net[(slash + 1)..] : net;
    }
}
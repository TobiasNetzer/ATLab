using System.Collections.Generic;
using ATLab.FixtureDesigner.Plates;
using System.Globalization;
using ATLab.FixtureDesigner.Fixture;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using ATLab.FixtureDesigner.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.FixtureDesigner.ViewModels;

/// <summary>Pad of the board in the pad list.</summary>
public partial class PadRow(Pad pad) : ObservableObject
{
    public Pad Pad { get; } = pad;
    public string Reference => Pad.Footprint.Reference;
    public string Number => Pad.Number;
    public string Net => Pad.HasNet ? Probe.ShortNetName(Pad.Net) : string.Empty;
    public string FullNet => Pad.Net;

    /// <summary>Footprint name without the library, e.g. TestPoint_Pad_D1.0mm.</summary>
    public string Footprint
    {
        get
        {
            var library = Pad.Footprint.Library;
            var colon = library.IndexOf(':');
            return colon >= 0 ? library[(colon + 1)..] : library;
        }
    }

    public string Kind => Pad.Kind == PadKind.Smd ? "SMD" : "THT";

    public string Size => string.Format(CultureInfo.CurrentCulture, "{0:0.##} × {1:0.##}", Pad.Width, Pad.Height);

    /// <summary>Sides a probe can reach the pad from.</summary>
    public string Access
    {
        get
        {
            var top = Pad.IsAccessibleFrom(BoardSide.Top);
            var bottom = Pad.IsAccessibleFrom(BoardSide.Bottom);
            return (top, bottom) switch
            {
                (true, true) => Localizer.Get("Access_Both"),
                (true, false) => Localizer.Enum(BoardSide.Top),
                (false, true) => Localizer.Enum(BoardSide.Bottom),
                _ => Localizer.Get("Access_None")
            };
        }
    }

    public bool IsTestPoint => Pad.Footprint.IsTestPoint;

    /// <summary>Probes on this pad, e.g. "P3".</summary>
    [ObservableProperty] private string _probes = string.Empty;

    public void RefreshTexts() => OnPropertyChanged(nameof(Access));
}

/// <summary>Pin of the adapter's DUT connector in the adapter list, with the probes wired to it.</summary>
public partial class AdapterRow(AdapterPin pin) : ObservableObject
{
    public AdapterPin Pin { get; } = pin;
    public string PinName => Pin.Pin;
    public string Resource => Pin.Resource;
    public string Signal => Pin.Signal;

    [ObservableProperty] private string _probes = string.Empty;
    [ObservableProperty] private string _nets = string.Empty;
    [ObservableProperty] private bool _isUsed;

    /// <summary>Probes of different nets on this pin.</summary>
    [ObservableProperty] private bool _isShort;
}

/// <summary>Entry of the pin selection of a probe: the pin and the probes already wired to it.</summary>
public partial class PinOption(AdapterPin pin) : ObservableObject
{
    public AdapterPin Pin { get; } = pin;
    public string Display => Pin.Display;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUsed))]
    private string _usedBy = string.Empty;

    public bool IsUsed => UsedBy.Length > 0;

    public override string ToString() => Display;
}

/// <summary>
/// A plate of the fixture in the Manufacturing tab: whether the fixture has it, whether it has an outline of its own,
/// its thickness, whether it is exported, and what it contains.
/// </summary>
public partial class PlateRow(FixturePlate plate) : ObservableObject
{
    public FixturePlate Plate { get; } = plate;

    public string Name => PlateRow.NameOf(Plate);

    public static string NameOf(FixturePlate plate) => Localizer.Get("Plate_" + plate);

    /// <summary>The fixture has this plate: the top probe plate only with top probes.</summary>
    [ObservableProperty] private bool _isPresent;

    [ObservableProperty] private bool _hasOwnOutline;

    /// <summary>Thickness in the STEP file, mm.</summary>
    [ObservableProperty] private double _thickness = FixtureFile.DefaultPlateThickness;

    [ObservableProperty] private bool _isExported = true;

    /// <summary>Size and holes of the plate.</summary>
    [ObservableProperty] private string _summary = string.Empty;

    public void RefreshTexts() => OnPropertyChanged(nameof(Name));
}

/// <summary>A side of the plate a pocket can be milled from, as offered in the list of shapes.</summary>
public sealed record PocketSideOption(PocketSide Side)
{
    public static IReadOnlyList<PocketSideOption> All { get; } = [new(PocketSide.BoardSide), new(PocketSide.FarSide)];

    public string Label => Localizer.Get("PocketSide_" + Side);
}

/// <summary>
/// A circle or cut-out of an imported outline in the Manufacturing tab: through the plate, or a pocket milled to a depth
/// from one side of it.
/// </summary>
public partial class OutlineShapeRow(bool isCircle, int index, string name) : ObservableObject
{
    public bool IsCircle { get; } = isCircle;

    /// <summary>Index into the circles or the cut-outs of the outline.</summary>
    public int Index { get; } = index;

    public string Name { get; } = name;

    public IReadOnlyList<PocketSideOption> Sides => PocketSideOption.All;

    [ObservableProperty] private bool _isPocket;

    /// <summary>Depth of the pocket, mm.</summary>
    [ObservableProperty] private double _depth = 2;

    [ObservableProperty] private PocketSideOption _side = PocketSideOption.All[0];

    public Pocket? Pocket => IsPocket && Depth > 0 ? new Pocket(Depth, Side.Side) : null;
}

/// <summary>An outline that can be edited: the one the plates share, or a plate's own.</summary>
public sealed record OutlineTarget(FixturePlate? Plate)
{
    public string Label => Plate is { } plate ? PlateRow.NameOf(plate) : Localizer.Get("Outline_Shared");
}

/// <summary>Language in the selection box, in its own language (English, Deutsch).</summary>
public sealed record LanguageOption(CultureInfo Culture)
{
    public override string ToString() =>
        Culture.NativeName.Length > 0 ? char.ToUpper(Culture.NativeName[0], Culture) + Culture.NativeName[1..] : Culture.Name;
}
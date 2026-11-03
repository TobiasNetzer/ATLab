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

/// <summary>Language in the selection box, in its own language (English, Deutsch).</summary>
public sealed record LanguageOption(CultureInfo Culture)
{
    public override string ToString() =>
        Culture.NativeName.Length > 0 ? char.ToUpper(Culture.NativeName[0], Culture) + Culture.NativeName[1..] : Culture.Name;
}
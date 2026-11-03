using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace ATLab.Pcb.Controls;

/// <summary>
/// Colors of the KiCad layers in the board view: KiCad's default colors on the dark background, darker ones on the
/// light background, so silkscreen and courtyards stay visible.
/// </summary>
public static class LayerColors
{
    /// <summary>
    /// Pseudo layers for the pads with copper on the top or the bottom side and for the holes, so they can be hidden
    /// and colored like real layers.
    /// </summary>
    public const string TopPads = "@PadsTop";

    public const string BottomPads = "@PadsBottom";

    /// <summary>The pads layer of a side.</summary>
    public static string PadsOf(ATLab.Pcb.Kicad.BoardSide side) => side == ATLab.Pcb.Kicad.BoardSide.Top ? TopPads : BottomPads;

    public const string Holes = "@Holes";

    public static Color Get(string layer, bool dark) => Color.Parse(Hex(layer, dark));

    /// <summary>The color the user chose for a layer, the same on both backgrounds, else the default one.</summary>
    public static Color Get(string layer, bool dark, IReadOnlyDictionary<string, Color>? chosen) =>
        chosen is not null && chosen.TryGetValue(layer, out var color) ? color : Get(layer, dark);

    /// <summary>Chosen colors as stored in the settings ("#RRGGBB"), the unreadable ones left out.</summary>
    public static Dictionary<string, Color>? Parse(IReadOnlyDictionary<string, string>? stored) =>
        stored?.Where(p => Color.TryParse(p.Value, out _)).ToDictionary(p => p.Key, p => Color.Parse(p.Value));

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string Hex(string layer, bool dark) => layer switch
    {
        "F.Cu" => dark ? "#C83434" : "#B4282C",
        "B.Cu" => dark ? "#4D7FC4" : "#2E5CB8",
        "In1.Cu" => dark ? "#7FC87F" : "#3C8C3C",
        "In2.Cu" => dark ? "#CE7D2C" : "#A85A10",
        "In3.Cu" => dark ? "#4FCBCB" : "#1F8C8C",
        "In4.Cu" => dark ? "#DB628B" : "#B03A63",
        _ when layer.EndsWith(".Cu", System.StringComparison.Ordinal) => dark ? "#A0A0A0" : "#707070",
        "F.SilkS" => dark ? "#F2EDA1" : "#8C7D00",
        "B.SilkS" => dark ? "#E8B2A7" : "#A0524B",
        "F.Mask" => dark ? "#D864FF" : "#9B30C8",
        "B.Mask" => dark ? "#02FFEE" : "#009C92",
        "F.Paste" => dark ? "#B5B5B5" : "#7A7A7A",
        "B.Paste" => dark ? "#00C2C2" : "#008080",
        "F.Fab" => dark ? "#AFAFAF" : "#6E6E6E",
        "B.Fab" => dark ? "#8A90C8" : "#3C4170",
        "F.CrtYd" => dark ? "#FF26E2" : "#B000A0",
        "B.CrtYd" => dark ? "#26E9FF" : "#008C9E",
        "Cmts.User" => dark ? "#5994DC" : "#2C64B0",
        "Eco1.User" => dark ? "#B4DBD2" : "#4C8C7E",
        "Eco2.User" => dark ? "#D8C852" : "#8C7C10",
        "Margin" => dark ? "#FF26E2" : "#B000A0",
        TopPads => dark ? "#F59E0B" : "#C2410C",
        BottomPads => dark ? "#60A5FA" : "#2563EB",
        Holes => dark ? "#A1A1AA" : "#71717A",
        _ => dark ? "#C2C2C2" : "#6E6E6E"
    };
}
using System;
using System.Collections.Generic;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;

namespace ATLab.FixtureDesigner.Fixture;

/// <summary>What gets a color of its own in the board view and the build sheet drawings.</summary>
public enum ColorItem
{
    Probe,
    LocatingPin,
    PreCenteringPin,
    SupportPin,
    PushRod,
    ProbePlate,
    MovingPlate,
    PressurePlate
}

/// <summary>
/// Colors ("#RRGGBB") of the probes, pins, rods and plate outlines, the same in the board view and the build sheet. The
/// user's choice comes from the settings; everything else keeps its default.
/// </summary>
public static class FixtureColors
{
    private static readonly Dictionary<ColorItem, string> Defaults = new()
    {
        [ColorItem.Probe] = MarkerColors.Probe,
        [ColorItem.LocatingPin] = MarkerColors.LocatingPin,
        [ColorItem.PreCenteringPin] = MarkerColors.PreCenteringPin,
        [ColorItem.SupportPin] = MarkerColors.SupportPin,
        [ColorItem.PushRod] = MarkerColors.PushRod,
        [ColorItem.ProbePlate] = "#52525B",
        [ColorItem.MovingPlate] = "#EA580C",
        [ColorItem.PressurePlate] = "#2563EB"
    };

    private static readonly Dictionary<ColorItem, string> Current = new(Defaults);

    public static string Get(ColorItem item) => Current[item];

    public static string Default(ColorItem item) => Defaults[item];

    public static void Set(ColorItem item, string hex) => Current[item] = hex;

    /// <summary>The colors the user chose (enum name → "#RRGGBB"); unknown names and bad values are skipped.</summary>
    public static void Load(IReadOnlyDictionary<string, string>? stored)
    {
        foreach (var (item, hex) in Defaults)
            Current[item] = hex;

        foreach (var (name, hex) in stored ?? new Dictionary<string, string>())
            if (Enum.TryParse<ColorItem>(name, out var item) && Avalonia.Media.Color.TryParse(hex, out _))
                Current[item] = hex;
    }

    /// <summary>The colors that differ from the defaults, for the settings; null if none does.</summary>
    public static Dictionary<string, string>? Changed()
    {
        var changed = new Dictionary<string, string>();
        foreach (var (item, hex) in Current)
            if (!string.Equals(hex, Defaults[item], StringComparison.OrdinalIgnoreCase))
                changed[item.ToString()] = hex;
        return changed.Count > 0 ? changed : null;
    }

    public static ColorItem Of(FixturePlate plate) => plate switch
    {
        FixturePlate.MovingPlate => ColorItem.MovingPlate,
        FixturePlate.PressurePlate => ColorItem.PressurePlate,
        _ => ColorItem.ProbePlate
    };

    public static ColorItem Of(FixturePartKind kind) => kind switch
    {
        FixturePartKind.LocatingPin => ColorItem.LocatingPin,
        FixturePartKind.PreCenteringPin => ColorItem.PreCenteringPin,
        FixturePartKind.SupportPin => ColorItem.SupportPin,
        _ => ColorItem.PushRod
    };
}

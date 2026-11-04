using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Kicad;
using ATLab.FixtureDesigner.Localization;

using ATLab.Pcb.Fixture;

namespace ATLab.FixtureDesigner.Fixture;

public enum CheckSeverity
{
    Error,
    Warning,
    Info
}

/// <summary>Result of a check, with the probes it is about.</summary>
public sealed record CheckResult(CheckSeverity Severity, string Message, IReadOnlyList<Probe> Probes)
{
    public bool IsError => Severity == CheckSeverity.Error;
    public bool IsWarning => Severity == CheckSeverity.Warning;
    public bool IsInfo => Severity == CheckSeverity.Info;
    public string ProbeNames => string.Join(", ", Probes.Select(p => p.Name));
}

/// <summary>
/// Checks the probes and their wiring: pads that cannot be reached, shorts between nets, interfaces and stimulus
/// channels that are only partly wired, and changes of the board since the fixture was saved.
/// </summary>
public static class FixtureChecks
{
    /// <summary>A pad moved further than this since the fixture was saved is reported.</summary>
    private const double MoveTolerance = 0.01;

    public static List<CheckResult> Run(IReadOnlyList<Probe> probes, AdapterPinout pinout, Board? board = null,
        IReadOnlyList<FixturePart>? parts = null)
    {
        var results = new List<CheckResult>();
        parts ??= [];

        CheckPads(probes, results);
        CheckDuplicates(probes, results);
        CheckShorts(probes, results);
        CheckUnwired(probes, results);
        CheckMeasChannelsPerNet(probes, results);
        CheckInterfaces(probes, pinout, results);
        CheckExternalStimulus(probes, results);
        CheckStimulus(probes, results);
        CheckProbeTypes(probes, results);
        CheckSpacing(probes, results);
        CheckPadSize(probes, results);
        if (board is not null)
            CheckComponents(probes, board, results);
        CheckParts(parts, probes, board, results);

        // Errors first, then by the first probe concerned
        return results
            .OrderBy(r => r.Severity)
            .ThenBy(r => r.Probes.Count > 0 ? r.Probes.Min(p => p.Number) : int.MaxValue)
            .ToList();
    }

    /// <summary>Sets every probe's worst severity, for the colors in the list and on the board.</summary>
    public static void Mark(IReadOnlyList<Probe> probes, IReadOnlyList<CheckResult> results)
    {
        foreach (var probe in probes)
        {
            var severities = results.Where(r => r.Probes.Contains(probe)).Select(r => r.Severity).ToList();
            probe.Severity = severities.Count > 0 ? severities.Min() : null;
        }
    }

    private static string SideName(BoardSide side) => Localizer.Get(side == BoardSide.Top ? "Side_Top" : "Side_Bottom");

    private static void CheckPads(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        foreach (var probe in probes)
        {
            if (probe.Pad is not { } pad)
            {
                results.Add(new(CheckSeverity.Error, Localizer.Format("Check_PadMissing", probe.Name, probe.PadDisplay), [probe]));
                continue;
            }

            if (!pad.HasCopperOn(probe.Side))
                results.Add(new(CheckSeverity.Error,
                    Localizer.Format("Check_NoCopper", probe.Name, probe.PadDisplay, SideName(probe.Side)), [probe]));
            else if (!pad.IsAccessibleFrom(probe.Side))
                results.Add(new(CheckSeverity.Error,
                    Localizer.Format("Check_MaskCovered", probe.Name, probe.PadDisplay, SideName(probe.Side)), [probe]));

            if (!pad.HasNet)
                results.Add(new(CheckSeverity.Warning, Localizer.Format("Check_NoNet", probe.Name, probe.PadDisplay), [probe]));

            if (probe.SavedNet.Length > 0 && pad.Net != probe.SavedNet)
                results.Add(new(CheckSeverity.Warning,
                    Localizer.Format("Check_NetChanged", probe.Name, probe.PadDisplay, Probe.ShortNetName(probe.SavedNet),
                        probe.ShortNet), [probe]));

            if (probe.SavedPosition is { } saved && saved.DistanceTo(pad.Position) > MoveTolerance)
                results.Add(new(CheckSeverity.Warning,
                    Localizer.Format("Check_PadMoved", probe.Name, probe.PadDisplay, saved.DistanceTo(pad.Position)), [probe]));
        }
    }

    private static void CheckDuplicates(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        foreach (var group in probes.GroupBy(p => (p.Key, p.Side)).Where(g => g.Count() > 1))
        {
            var list = group.ToList();
            results.Add(new(CheckSeverity.Error,
                Localizer.Format("Check_DuplicateProbe", Names(list), list[0].PadDisplay, SideName(list[0].Side)), list));
        }
    }

    /// <summary>Probes on different nets wired to the same pin would short these nets.</summary>
    private static void CheckShorts(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        foreach (var group in probes.Where(p => p.Pin is not null).GroupBy(p => p.Pin!))
        {
            var nets = group.Where(p => p.Net.Length > 0).GroupBy(p => p.Net).ToList();
            if (nets.Count < 2)
                continue;

            var detail = string.Join(", ", nets.Select(n => $"{Probe.ShortNetName(n.Key)} ({Names(n)})"));
            results.Add(new(CheckSeverity.Error, Localizer.Format("Check_Short", group.Key.Display, detail), group.ToList()));
        }
    }

    private static void CheckUnwired(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        foreach (var probe in probes.Where(p => p.Pin is null))
            results.Add(new(CheckSeverity.Warning,
                Localizer.Format("Check_NotWired", probe.Name, probe.PadDisplay, probe.ShortNet), [probe]));
    }

    /// <summary>One net on several matrix channels uses up channels, usually by mistake.</summary>
    private static void CheckMeasChannelsPerNet(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        var groups = probes
            .Where(p => p.Pin?.Kind == ResourceKind.Meas && p.Net.Length > 0)
            .GroupBy(p => p.Net);

        foreach (var group in groups)
        {
            var pins = group.Select(p => p.Pin!).Distinct().OrderBy(p => p.Channel).ToList();
            if (pins.Count < 2)
                continue;

            results.Add(new(CheckSeverity.Warning,
                Localizer.Format("Check_NetOnSeveralChannels", Probe.ShortNetName(group.Key), string.Join(", ", pins.Select(p => p.Resource))),
                group.ToList()));
        }
    }

    /// <summary>An isolated interface needs all its lines, including VREF and GND from the DUT.</summary>
    private static void CheckInterfaces(IReadOnlyList<Probe> probes, AdapterPinout pinout, List<CheckResult> results)
    {
        var used = probes.Where(p => p.Pin?.Kind == ResourceKind.Interface).GroupBy(p => p.Pin!.Interface);
        foreach (var group in used)
        {
            var all = pinout.FixturePins.Where(p => p.Kind == ResourceKind.Interface && p.Interface == group.Key).ToList();
            var wired = group.Select(p => p.Pin!).Distinct().ToHashSet();
            var missing = all.Where(p => !wired.Contains(p)).Select(p => p.Role).ToList();
            if (missing.Count == 0)
                continue;

            results.Add(new(CheckSeverity.Warning,
                Localizer.Format("Check_InterfaceIncomplete", all[0].InterfaceName, string.Join(", ", missing)), group.ToList()));
        }
    }

    /// <summary>An external stimulus is switched with both lines; one without the other has no return path.</summary>
    private static void CheckExternalStimulus(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        foreach (var group in probes.Where(p => p.Pin?.Kind == ResourceKind.ExtStim).GroupBy(p => p.Pin!.Channel))
        {
            var polarities = group.Select(p => p.Pin!.Polarity).Distinct().ToList();
            if (polarities.Count == 2)
                continue;

            var missing = polarities.Contains("H") ? "L" : "H";
            results.Add(new(CheckSeverity.Warning,
                Localizer.Format("Check_ExtStimIncomplete", group.Key, missing), group.ToList()));
        }
    }

    /// <summary>
    /// A stimulation channel is a relay contact between In and Out. With only one of them on a probe, the other must be
    /// wired inside the fixture, e.g. to a supply.
    /// </summary>
    private static void CheckStimulus(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        foreach (var group in probes.Where(p => p.Pin?.Kind == ResourceKind.Stim).GroupBy(p => p.Pin!.Channel))
        {
            var terminals = group.Select(p => p.Pin!.Terminal).Distinct().ToList();
            if (terminals.Count == 2)
                continue;

            var used = terminals[0] ?? string.Empty;
            var other = used == "In" ? "Out" : "In";
            results.Add(new(CheckSeverity.Info,
                Localizer.Format("Check_StimOneTerminal", group.Key, used, other), group.ToList()));
        }
    }

    private static string Mm(double value) => value.ToString("0.00", System.Globalization.CultureInfo.CurrentCulture);

    /// <summary>Without a type, a probe has no dimensions for the checks and no hole in the plate files.</summary>
    private static void CheckProbeTypes(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        var untyped = probes.Where(p => p.Type is null).ToList();
        if (untyped.Count > 0)
            results.Add(new(CheckSeverity.Warning, Localizer.Format("Check_NoProbeType", Names(untyped)), untyped));

        foreach (var group in probes.Where(p => p.Type is { DrillDiameter: <= 0 }).GroupBy(p => p.Type!))
            results.Add(new(CheckSeverity.Warning, Localizer.Format("Check_NoDrill", group.Key.Name), group.ToList()));
    }

    /// <summary>Probes of one plate closer than their types allow: the receptacles do not fit next to each other.</summary>
    private static void CheckSpacing(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        foreach (var side in new[] { BoardSide.Top, BoardSide.Bottom })
        {
            var list = probes.Where(p => p.Side == side && p.Pad is not null).ToList();
            for (var i = 0; i < list.Count; i++)
            for (var j = i + 1; j < list.Count; j++)
            {
                var a = list[i];
                var b = list[j];
                var required = Math.Max(a.Type?.MinSpacing ?? 0, b.Type?.MinSpacing ?? 0);
                var distance = a.Pad!.Position.DistanceTo(b.Pad!.Position);
                if (required > 0 && distance < required - 1e-6)
                    results.Add(new(CheckSeverity.Error,
                        Localizer.Format("Check_Spacing", a.Name, b.Name, Mm(distance), Mm(required)), [a, b]));
            }
        }
    }

    private static void CheckPadSize(IReadOnlyList<Probe> probes, List<CheckResult> results)
    {
        foreach (var probe in probes.Where(p => p.Pad is not null && p.Type is { MinPadSize: > 0 }))
            if (probe.Pad!.MinSize < probe.Type!.MinPadSize - 1e-6)
                results.Add(new(CheckSeverity.Warning,
                    Localizer.Format("Check_PadTooSmall", probe.Name, probe.PadDisplay, Mm(probe.Pad.MinSize), probe.Type.Name,
                        Mm(probe.Type.MinPadSize)), [probe]));
    }

    /// <summary>
    /// A probe on or near another component of its side: the probe or its receptacle could hit the part. Uses the
    /// courtyards of the footprints; the footprint of the probed pad itself does not count.
    /// </summary>
    private static void CheckComponents(IReadOnlyList<Probe> probes, Board board, List<CheckResult> results)
    {
        foreach (var probe in probes.Where(p => p.Pad is not null))
        {
            var clearance = probe.Type?.ComponentClearance ?? 0;
            var position = probe.Pad!.Position;
            var nearest = (Footprint: (Footprint?)null, Distance: double.PositiveInfinity);

            foreach (var footprint in board.Footprints)
            {
                if (footprint == probe.Pad.Footprint || footprint.Side != probe.Side || !footprint.HasCourtyard)
                    continue;

                var b = footprint.CourtyardBounds;
                if (position.X < b.MinX - clearance || position.X > b.MaxX + clearance ||
                    position.Y < b.MinY - clearance || position.Y > b.MaxY + clearance)
                    continue;

                var distance = footprint.DistanceToCourtyard(position);
                if (distance < nearest.Distance)
                    nearest = (footprint, distance);
            }

            if (nearest.Footprint is not { } hit)
                continue;

            if (nearest.Distance <= 0)
                results.Add(new(CheckSeverity.Warning, Localizer.Format("Check_OnComponent", probe.Name, hit.Reference), [probe]));
            else if (nearest.Distance < clearance - 1e-6)
                results.Add(new(CheckSeverity.Warning,
                    Localizer.Format("Check_NearComponent", probe.Name, Mm(nearest.Distance), hit.Reference, Mm(clearance)), [probe]));
        }
    }

    /// <summary>
    /// Locating pins stand in a hole of the board that they fit, with room to the openings of the bottom probes; pre-centering
    /// pins stand beside the board, their type's gap away from its edge; support pins and push rods are under the board and keep their
    /// clearance from the components of their side. Every part needs a type with a drill diameter. The worst result
    /// becomes the part's state, which colors it on the board.
    /// </summary>
    private static void CheckParts(IReadOnlyList<FixturePart> parts, IReadOnlyList<Probe> probes, Board? board, List<CheckResult> results)
    {
        foreach (var part in parts)
        {
            var state = BoardMarkerState.Normal;
            void Add(CheckSeverity severity, string message)
            {
                results.Add(new(severity, message, []));
                if (severity == CheckSeverity.Error)
                    state = BoardMarkerState.Error;
                else if (severity == CheckSeverity.Warning && state == BoardMarkerState.Normal)
                    state = BoardMarkerState.Warning;
            }

            if (part.Type is not { } type)
            {
                Add(CheckSeverity.Warning, Localizer.Format("Check_PartNoType", part.Name));
                part.State = state;
                continue;
            }

            if (type.DrillDiameter <= 0)
                Add(CheckSeverity.Error, Localizer.Format("Check_PartNoDrill", part.Name, type.Name));

            if (board is not null && part.Kind == FixturePartKind.LocatingPin)
            {
                CheckLocatingPin(part, type, probes, board, Add);
                part.State = state;
                continue;
            }

            if (board is null || board.Outline.Count == 0)
            {
                part.State = state;
                continue;
            }

            var center = new Point2(part.BoardX, part.BoardY);
            var r = Math.Max(type.Diameter, 0) / 2;
            var inside = BoardEdge.Contains(board, center);
            var toEdge = BoardEdge.Distance(board, center);

            // A pre-centering pin stands beside the board, its type's gap away from the edge
            if (part.Kind == FixturePartKind.PreCenteringPin)
            {
                var gap = Math.Max(type.EdgeGap, 0);
                if (inside || toEdge < r - BoardEdge.TouchTolerance)
                    Add(CheckSeverity.Error, Localizer.Format("Check_PreCenteringOnBoard", part.Name));
                else if (Math.Abs(toEdge - r - gap) > BoardEdge.TouchTolerance)
                    Add(CheckSeverity.Warning, Localizer.Format("Check_PreCenteringGap", part.Name, Mm(toEdge - r), Mm(gap)));
            }
            else if (!inside)
            {
                Add(CheckSeverity.Error, Localizer.Format("Check_PartOffBoard", part.Name));
            }
            else
            {
                // Clearance from the edge of the pin or rod to the courtyards of the components on its side
                var side = part.PlateSide;
                // (No component with a courtyard on that side: nothing to keep clear of)
                var nearest = board.Footprints
                    .Where(f => f.Side == side && f.HasCourtyard)
                    .Select(f => (Footprint: (Footprint?)f, Distance: f.DistanceToCourtyard(center) - r))
                    .OrderBy(f => f.Distance)
                    .FirstOrDefault();

                if (nearest.Footprint is { } hit)
                {
                    if (nearest.Distance <= 0)
                        Add(CheckSeverity.Error, Localizer.Format("Check_PartOnComponent", part.Name, hit.Reference));
                    else if (nearest.Distance < type.ComponentClearance - 1e-6)
                        Add(CheckSeverity.Warning, Localizer.Format("Check_PartNearComponent", part.Name, Mm(nearest.Distance),
                            hit.Reference, Mm(type.ComponentClearance)));
                }
            }

            part.State = state;
        }
    }

    /// <summary>
    /// A locating pin needs a hole of the board at its place that it fits through, and its hole in the moving plate must
    /// not run into the opening of a bottom probe. It stands in its own component's hole, so courtyards do not count.
    /// </summary>
    private static void CheckLocatingPin(FixturePart part, PartType type, IReadOnlyList<Probe> probes, Board board,
        Action<CheckSeverity, string> add)
    {
        var center = new Point2(part.BoardX, part.BoardY);
        if (BoardHoles.At(board, center) is not { } hole)
        {
            add(CheckSeverity.Error, Localizer.Format("Check_LocatingNoHole", part.Name));
            return;
        }

        if (type.Diameter > hole.Diameter + 1e-6)
            add(CheckSeverity.Error, Localizer.Format("Check_LocatingTooBig", part.Name, Mm(type.Diameter), hole.Reference, Mm(hole.Diameter)));

        foreach (var probe in probes.Where(p => p.HasOpening && p.Pad is not null && p.Opening > 0))
        {
            if (center.DistanceTo(probe.Pad!.Position) < (type.DrillDiameter + probe.Opening) / 2)
                add(CheckSeverity.Error, Localizer.Format("Check_LocatingPinOverlap", probe.Name, part.Name));
        }
    }

    private static string Names(IEnumerable<Probe> probes) => string.Join(", ", probes.Select(p => p.Name));
}
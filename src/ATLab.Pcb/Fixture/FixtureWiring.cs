using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.Pcb.Fixture;

/// <summary>
/// A part that sits in the wiring between the board and the test hardware, e.g. a series resistor or a relay: its
/// designator, what it is and the terminals wires can go to.
/// </summary>
public sealed class ComponentEntry
{
    public string Designator { get; set; } = string.Empty;

    /// <summary>What it is, e.g. "1k 0.25 W" or a part number, for the bill of materials.</summary>
    public string Description { get; set; } = string.Empty;

    public List<string> Terminals { get; set; } = [];
    public string Note { get; set; } = string.Empty;
}

/// <summary>A wire of the fixture between two points other than a probe and the pin it is assigned to.</summary>
public sealed class WireEntry
{
    public WireEnd From { get; set; } = new();
    public WireEnd To { get; set; } = new();
    public string Note { get; set; } = string.Empty;
}

/// <summary>One end of a wire: a probe (by number), a pin of the test hardware or a terminal of a component.</summary>
public sealed class WireEnd
{
    public int? Probe { get; set; }
    public string? Pin { get; set; }
    public string? Component { get; set; }
    public string? Terminal { get; set; }

    /// <summary>Key of the point in the connection groups; null for an end that is not set.</summary>
    public string? Key => Probe is { } probe ? ProbeKey(probe)
        : Pin is { Length: > 0 } pin ? PinKey(pin)
        : Component is { Length: > 0 } component && Terminal is { Length: > 0 } terminal ? TerminalKey(component, terminal)
        : null;

    public static string ProbeKey(int number) => "P:" + number;
    public static string PinKey(string pin) => "J:" + pin.ToLowerInvariant();
    public static string TerminalKey(string component, string terminal) => "T:" + component + "." + terminal;
}

/// <summary>
/// Points joined by wires, found by union-find. Points are only conductively joined by wires (and a probe's own pin);
/// a component never joins its terminals, so whatever sits behind a component is on another net.
/// </summary>
public sealed class ConnectionGroups<T> where T : notnull
{
    private readonly Dictionary<T, T> _parent = [];

    public void Connect(T a, T b) => _parent[Find(a)] = Find(b);

    public bool Contains(T point) => _parent.ContainsKey(point);

    public T Find(T point)
    {
        if (!_parent.TryGetValue(point, out var parent))
        {
            _parent[point] = point;
            return point;
        }

        if (EqualityComparer<T>.Default.Equals(parent, point))
            return point;

        var root = Find(parent);
        _parent[point] = root;
        return root;
    }

    /// <summary>The groups of joined points; points that were never connected are not in any.</summary>
    public IReadOnlyList<List<T>> Groups() =>
        _parent.Keys.GroupBy(Find).Select(g => g.ToList()).ToList();
}

/// <summary>Net on a pin of the test hardware as the fixture wires it, for ATLab's board link.</summary>
public sealed record FixturePinNet(string Pin, string Net, IReadOnlyList<ProbeEntry> Probes);

public static class FixtureWiring
{
    /// <summary>
    /// The pins that wires join to probes, with the net of these probes. A pin that reaches the board only through a
    /// component is on a net of its own and is not listed.
    /// </summary>
    public static IReadOnlyList<FixturePinNet> PinNets(FixtureFile file)
    {
        var groups = new ConnectionGroups<string>();
        foreach (var probe in file.Probes.Where(p => !string.IsNullOrEmpty(p.Pin)))
            groups.Connect(WireEnd.ProbeKey(probe.Number), WireEnd.PinKey(probe.Pin!));
        foreach (var wire in file.Wires)
            if (wire.From.Key is { } from && wire.To.Key is { } to)
                groups.Connect(from, to);

        var probes = file.Probes.ToDictionary(p => WireEnd.ProbeKey(p.Number), p => p);
        var pins = new Dictionary<string, string>();
        foreach (var probe in file.Probes.Where(p => !string.IsNullOrEmpty(p.Pin)))
            pins.TryAdd(WireEnd.PinKey(probe.Pin!), probe.Pin!);
        foreach (var end in file.Wires.SelectMany(w => new[] { w.From, w.To }).Where(e => e.Probe is null && !string.IsNullOrEmpty(e.Pin)))
            pins.TryAdd(WireEnd.PinKey(end.Pin!), end.Pin!);

        var result = new List<FixturePinNet>();
        foreach (var group in groups.Groups())
        {
            var onBoard = group.Where(probes.ContainsKey).Select(k => probes[k]).OrderBy(p => p.Number).ToList();
            if (onBoard.Count == 0)
                continue;

            var net = onBoard.Select(p => p.Net).FirstOrDefault(n => n.Length > 0) ?? string.Empty;
            foreach (var key in group.Where(pins.ContainsKey))
                result.Add(new FixturePinNet(pins[key], net, onBoard));
        }

        return result.OrderBy(p => p.Pin, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

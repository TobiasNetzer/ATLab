using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Pcb.Fixture;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.FixtureDesigner.Fixture;

/// <summary>A component in the wiring of the fixture, e.g. a series resistor or a relay, while it is edited.</summary>
public partial class FixtureComponent : ObservableObject
{
    /// <summary>Designator of a new component until the user names it.</summary>
    public const string Unnamed = "?";

    [ObservableProperty] private string _designator = Unnamed;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _note = string.Empty;

    /// <summary>Terminals wires can go to, e.g. 1 and 2 or A1, A2, COM, NC, NO.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TerminalsText))]
    private IReadOnlyList<string> _terminals = [];

    /// <summary>The terminals before the last change, so wires can follow a renamed terminal.</summary>
    public IReadOnlyList<string> PreviousTerminals { get; private set; } = [];

    partial void OnTerminalsChanged(IReadOnlyList<string>? oldValue, IReadOnlyList<string> newValue) => PreviousTerminals = oldValue ?? [];

    /// <summary>The terminals as typed in the list, separated by commas.</summary>
    public string TerminalsText
    {
        get => string.Join(", ", Terminals);
        set => Terminals = ParseTerminals(value);
    }

    public static IReadOnlyList<string> ParseTerminals(string? text) =>
        (text ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Terminals no wire goes to, set by the checks.</summary>
    [ObservableProperty] private string _openTerminals = string.Empty;

    public string Label => Designator.Length > 0 ? Designator : Unnamed;

    public bool IsNamed => Designator.Length > 0 && Designator != Unnamed;
}

/// <summary>A terminal of a component as a point of the wiring.</summary>
public sealed record TerminalRef(FixtureComponent Component, string Terminal)
{
    public override string ToString() => $"{Component.Label}.{Terminal}";
}

/// <summary>One end of a wire: a probe, a pin of the test hardware or a terminal of a component.</summary>
public sealed record WireEndpoint
{
    private WireEndpoint(Probe? probe, AdapterPin? pin, TerminalRef? terminal)
    {
        Probe = probe;
        Pin = pin;
        Terminal = terminal;
    }

    public static WireEndpoint Of(Probe probe) => new(probe, null, null);
    public static WireEndpoint Of(AdapterPin pin) => new(null, pin, null);
    public static WireEndpoint Of(FixtureComponent component, string terminal) => new(null, null, new TerminalRef(component, terminal));

    public Probe? Probe { get; }
    public AdapterPin? Pin { get; }
    public TerminalRef? Terminal { get; }

    /// <summary>The point in the connection groups.</summary>
    public object Point => (object?)Probe ?? (object?)Pin ?? Terminal!;

    /// <summary>Short name, e.g. "P3", "c7" or "R1.2".</summary>
    public string Name => Probe?.Name ?? Pin?.Pin ?? Terminal!.ToString();

    /// <summary>What it is, e.g. the pad and net of a probe, the resource of a pin or the description of a component.</summary>
    public string Detail => Probe is { } probe ? $"{probe.PadDisplay} - {probe.ShortNet}"
        : Pin is { } pin ? pin.Resource
        : Terminal!.Component.Description;

    public string Display => Detail.Length > 0 ? $"{Name} · {Detail}" : Name;

    public override string ToString() => Display;
}

/// <summary>A wire of the fixture while it is edited.</summary>
public partial class Wire : ObservableObject
{
    [ObservableProperty] private WireEndpoint? _from;
    [ObservableProperty] private WireEndpoint? _to;
    [ObservableProperty] private string _note = string.Empty;

    /// <summary>Number in the list and the build sheet (W1, W2 …).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name))]
    private int _number;

    public string Name => $"W{Number}";

    public bool IsComplete => From is not null && To is not null;

    public IEnumerable<WireEndpoint> Ends => new[] { From, To }.OfType<WireEndpoint>();
}

/// <summary>Points joined by wires: the probes, pins and component terminals on one conductor.</summary>
public sealed class FixtureNet
{
    public List<Probe> Probes { get; } = [];
    public List<AdapterPin> Pins { get; } = [];
    public List<TerminalRef> Terminals { get; } = [];

    /// <summary>Nets of the board the probes touch; more than one is a short.</summary>
    public IReadOnlyList<string> BoardNets => Probes.Select(p => p.Net).Where(n => n.Length > 0).Distinct().ToList();

    /// <summary>Everything on it but the given point, e.g. "P3, R1.2", for the lists.</summary>
    public string Describe(object? except = null) => string.Join(", ",
        Probes.Where(p => p != except).OrderBy(p => p.Number).Select(p => p.Name)
            .Concat(Terminals.Where(t => !t.Equals(except)).Select(t => t.ToString()))
            .Concat(Pins.Where(p => p != except).Select(p => p.Pin)));

    /// <summary>Name for messages: its pins, else its terminals, else its probes.</summary>
    public string Label => Pins.Count > 0 ? string.Join(", ", Pins.Select(p => p.Display))
        : Terminals.Count > 0 ? string.Join(", ", Terminals)
        : string.Join(", ", Probes.Select(p => p.Name));
}

/// <summary>
/// The conductors of the fixture: every probe with its pin, joined with the wires. A component does not join its
/// terminals, so a pin behind a series resistor is on another net than the probe in front of it.
/// </summary>
public sealed class FixtureWiringModel
{
    private readonly Dictionary<object, FixtureNet> _netOf = [];

    public IReadOnlyList<FixtureNet> Nets { get; }

    private FixtureWiringModel(IReadOnlyList<FixtureNet> nets)
    {
        Nets = nets;
        foreach (var net in nets)
        {
            foreach (var probe in net.Probes)
                _netOf[probe] = net;
            foreach (var pin in net.Pins)
                _netOf[pin] = net;
            foreach (var terminal in net.Terminals)
                _netOf[terminal] = net;
        }
    }

    public static FixtureWiringModel Empty { get; } = new([]);

    public static FixtureWiringModel Build(IEnumerable<Probe> probes, IEnumerable<Wire> wires)
    {
        var groups = new ConnectionGroups<object>();
        foreach (var probe in probes.Where(p => p.Pin is not null))
            groups.Connect(probe, probe.Pin!);
        foreach (var wire in wires.Where(w => w.IsComplete && w.From != w.To))
            groups.Connect(wire.From!.Point, wire.To!.Point);

        var nets = new List<FixtureNet>();
        foreach (var group in groups.Groups())
        {
            var net = new FixtureNet();
            foreach (var point in group)
            {
                switch (point)
                {
                    case Probe probe: net.Probes.Add(probe); break;
                    case AdapterPin pin: net.Pins.Add(pin); break;
                    case TerminalRef terminal: net.Terminals.Add(terminal); break;
                }
            }

            net.Probes.Sort((a, b) => a.Number.CompareTo(b.Number));
            nets.Add(net);
        }

        return new FixtureWiringModel(nets);
    }

    /// <summary>The conductor of a probe, pin or terminal; null if nothing is wired to it.</summary>
    public FixtureNet? NetOf(object point) => _netOf.GetValueOrDefault(point);

    public bool IsWired(object point) => _netOf.ContainsKey(point);

    /// <summary>Net of the board on a pin: the one net of the probes wired to it; empty for none or a short.</summary>
    public string BoardNetOf(AdapterPin pin) => NetOf(pin)?.BoardNets is { Count: 1 } nets ? nets[0] : string.Empty;
}

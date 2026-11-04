using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.BoundaryScan.Bsdl;
using ATLab.Enums;
using ATLab.Models;

namespace ATLab.BoundaryScan.Interconnect;

/// <summary>
/// A net the test drives: the pins that drive it in turn, one per round, and the pins that read it. Every pin that can
/// drive is a driver, so a defective output is found too; a pin without output enable is the only driver, as it
/// cannot be switched off.
/// </summary>
public sealed record InterconnectDriveNet(
    InterconnectNetInfo Info,
    IReadOnlyList<InterconnectPin> Drivers,
    IReadOnlyList<InterconnectPin> Receivers)
{
    /// <summary>The pin that drives the net in a round; a net with fewer drivers than rounds starts over.</summary>
    public InterconnectPin DriverIn(int round) => Drivers[round % Drivers.Count];

    /// <summary>Every pin of the net with an output: switched off before the driver of a round is switched on.</summary>
    public IEnumerable<InterconnectPin> Outputs => Info.Pins.Where(p => p.CanDrive);
}

/// <summary>A net that must stay at one level: the pins that read it and the pins that must not drive it.</summary>
public sealed record InterconnectExpectNet(
    InterconnectNetInfo Info,
    bool Level,
    IReadOnlyList<InterconnectPin> Released,
    IReadOnlyList<InterconnectPin> Readers);

/// <summary>
/// The interconnect test of the configured nets. Nets in their safe state are not touched. A plan with problems must
/// not be run: a problem means a pin would drive against another output.
/// <para>
/// Every drive net gets a code of its own (1 … n). A round drives the bits of the codes and then their complements, so
/// every net is driven 0 and 1, and two shorted nets or an open receiver read a code that is not theirs: that is
/// 2 × ceil(log2(n + 2)) patterns per round. There is one round per driver of the net with the most drivers, so every
/// pin that can drive drives its net once.
/// </para>
/// </summary>
public sealed class InterconnectPlan
{
    public IReadOnlyList<InterconnectDriveNet> DriveNets { get; }
    public IReadOnlyList<InterconnectExpectNet> ExpectNets { get; }

    /// <summary>Problems that prevent the test, by net.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Problems { get; }

    /// <summary>Hints that do not prevent the test, e.g. a net only its driver reads, by net.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Notes { get; }

    public bool HasProblems => Problems.Count > 0;

    private Dictionary<string, string> _groupNames = new(StringComparer.Ordinal);

    /// <summary>The name problems and notes of a net are filed under: "A + B" for connected nets, else the net.</summary>
    public string GroupName(string net) => _groupNames.GetValueOrDefault(net, net);

    /// <summary>Nets connected with each other, each group sorted by name; nets without connection are groups of one.</summary>
    private static List<List<string>> Groups(IEnumerable<BoundaryScanInterconnectNet> entries)
    {
        var list = entries.ToList();
        var neighbours = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        void Link(string a, string b) => (neighbours.TryGetValue(a, out var s) ? s : neighbours[a] = []).Add(b);

        foreach (var entry in list)
        {
            Link(entry.Net, entry.Net);
            if (entry.ConnectedTo.Length > 0 && entry.ConnectedTo != entry.Net)
            {
                Link(entry.Net, entry.ConnectedTo);
                Link(entry.ConnectedTo, entry.Net);
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var groups = new List<List<string>>();
        foreach (var start in neighbours.Keys)
        {
            if (!seen.Add(start))
                continue;

            var group = new List<string> { start };
            for (var i = 0; i < group.Count; i++)
                foreach (var next in neighbours[group[i]].Where(seen.Add))
                    group.Add(next);

            groups.Add(group.Order(StringComparer.OrdinalIgnoreCase).ToList());
        }

        return groups;
    }
    public bool IsEmpty => DriveNets.Count == 0 && ExpectNets.Count == 0;

    /// <summary>Number of bits per code; a round has twice as many patterns.</summary>
    public int CodeBits { get; }

    public int Rounds => DriveNets.Count == 0 ? 0 : DriveNets.Max(n => n.Drivers.Count);
    public int PatternsPerRound => 2 * CodeBits;
    public int PatternCount => DriveNets.Count == 0 ? (ExpectNets.Count > 0 ? 1 : 0) : Rounds * PatternsPerRound;

    private InterconnectPlan(
        List<InterconnectDriveNet> driveNets,
        List<InterconnectExpectNet> expectNets,
        Dictionary<string, List<string>> problems,
        Dictionary<string, List<string>> notes)
    {
        DriveNets = driveNets;
        ExpectNets = expectNets;
        Problems = problems.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value);
        Notes = notes.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value);
        CodeBits = Math.Max(1, (int)Math.Ceiling(Math.Log2(driveNets.Count + 2)));
    }

    public static InterconnectPlan Create(IReadOnlyList<InterconnectNetInfo> nets, IEnumerable<BoundaryScanInterconnectNet> configured)
    {
        var byName = nets.ToDictionary(n => n.Net, StringComparer.Ordinal);
        var problems = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var notes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var driveNets = new List<InterconnectDriveNet>();
        var expectNets = new List<InterconnectExpectNet>();

        void Problem(string net, string text)
        {
            var list = problems.TryGetValue(net, out var l) ? l : problems[net] = [];
            if (!list.Contains(text))
                list.Add(text);
        }

        void Note(string net, string text) => (notes.TryGetValue(net, out var l) ? l : notes[net] = []).Add(text);

        // Connected nets (e.g. through a series resistor) are tested as one net with the pins of all of them
        var entries = configured.GroupBy(e => e.Net).Select(g => g.First()).ToDictionary(e => e.Net, StringComparer.Ordinal);
        var groupNames = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var group in Groups(entries.Values))
        {
            var roles = group.Select(n => entries.TryGetValue(n, out var e) ? e.Role : InterconnectNetRole.SAFE).Distinct().ToList();
            if (roles.All(r => r == InterconnectNetRole.SAFE))
                continue;

            var name = string.Join(" + ", group);
            foreach (var member in group)
                groupNames[member] = name;

            if (roles.Count > 1)
            {
                Problem(name, "The connected nets have different roles. Set the role of one of them again.");
                continue;
            }

            var role = roles[0];
            var present = group.Where(byName.ContainsKey).Select(n => byName[n]).ToList();
            if (present.Count == 0)
            {
                Problem(name, group.Count == 1
                    ? "The net is not on the board or has no boundary scan pin any more."
                    : "None of the connected nets is on the board with a boundary scan pin.");
                continue;
            }

            var info = present.Count == 1 && group.Count == 1
                ? present[0]
                : new InterconnectNetInfo(name, present.SelectMany(n => n.Pins).ToList(), present.SelectMany(n => n.OtherPads).ToList());

            var readers = info.Readers.ToList();
            if (readers.Count == 0)
                Problem(info.Net, "No boundary scan pin can read the net.");

            // A pin without output enable cannot be switched off: it drives anyway
            var alwaysDriving = info.Drivers.Where(p => !p.CanRelease).ToList();

            if (role == InterconnectNetRole.DRIVE)
            {
                if (!info.Drivers.Any())
                {
                    Problem(info.Net, "No boundary scan pin can drive the net.");
                    continue;
                }

                if (alwaysDriving.Count > 1)
                    Problem(info.Net, $"{string.Join(" and ", alwaysDriving)} always drive (no output enable), they would drive against each other.");

                // Drivers in chain order, so pins of one chip that share an output enable drive in the same round
                var drivers = alwaysDriving.Count > 0
                    ? alwaysDriving.Take(1).ToList()
                    : info.Drivers.ToList();

                if (alwaysDriving.Count == 1 && info.Drivers.Count() > 1)
                    Note(info.Net, $"Only {alwaysDriving[0]} drives the net: it has no output enable, so the other pins cannot drive while it does.");
                if (readers.Count > 0 && drivers.Count == 1 && readers.All(r => r == drivers[0]))
                    Note(info.Net, "This is the only boundary scan pin on the net: shorts and stuck nets are found, opens are not.");

                driveNets.Add(new InterconnectDriveNet(info, drivers, readers));
            }
            else
            {
                foreach (var pin in alwaysDriving)
                    Problem(info.Net, $"{pin} always drives (no output enable), the net cannot be read undisturbed.");

                expectNets.Add(new InterconnectExpectNet(info, role == InterconnectNetRole.EXPECT_HIGH, info.Drivers.ToList(), readers));
            }
        }

        var plan = new InterconnectPlan(driveNets, expectNets, problems, notes);

        // Output enables are often shared by several pins: switching on a driver switches all of them on, so in every
        // round each of them has to be a driver of that round, otherwise it would drive a net that is not set up for it
        for (var round = 0; round < plan.Rounds; round++)
        {
            var drivers = driveNets.Select(n => n.DriverIn(round)).ToList();
            foreach (var net in driveNets)
            {
                var driver = net.DriverIn(round);
                if (driver.Output!.ControlCell is not { } control || driver.Device.Bsdl is not { } bsdl)
                    continue;

                var shared = bsdl.BoundaryCells
                    .Where(c => c.ControlCell == control && c.Port != null && c.Function is BsdlCellFunction.Output3 or BsdlCellFunction.Bidir
                                && !string.Equals(c.Port, driver.Port, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Port!)
                    .Where(port => !drivers.Any(d => d.Is(driver.DeviceId, port)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (shared.Count > 0)
                {
                    var ports = string.Join(", ", shared.Take(6)) + (shared.Count > 6 ? " …" : "");
                    Problem(net.Info.Net, $"{driver} shares its output enable with {ports}, which would drive too. " +
                                          "Set their nets to Drive as well, or set this net to Safe.");
                }
            }
        }

        // The problems found by the round check are part of the plan as well
        return new InterconnectPlan(driveNets, expectNets, problems, notes) { _groupNames = groupNames };
    }

    public int RoundOf(int pattern) => pattern / PatternsPerRound;

    /// <summary>Level of drive net <paramref name="index"/> in pattern <paramref name="pattern"/> of the whole test.</summary>
    public bool DriveLevel(int index, int pattern)
    {
        var code = index + 1;
        var bit = pattern % PatternsPerRound;
        return bit < CodeBits
            ? ((code >> bit) & 1) == 1
            : ((code >> (bit - CodeBits)) & 1) == 0;
    }

    /// <summary>
    /// Compares what the pins read with what they should read. <paramref name="read"/> returns the level a pin read in
    /// a pattern of the whole test. Returns one message per fault.
    /// </summary>
    public List<string> Evaluate(Func<InterconnectPin, int, bool> read)
    {
        var faults = new List<string>();
        var rounds = Rounds;

        bool[] Vector(InterconnectPin pin, int round) =>
            Enumerable.Range(round * PatternsPerRound, PatternsPerRound).Select(p => read(pin, p)).ToArray();
        bool[] Expected(int index) => Enumerable.Range(0, PatternsPerRound).Select(p => DriveLevel(index, p)).ToArray();
        static string Bits(bool[] v) => string.Concat(v.Select(b => b ? '1' : '0'));
        static bool AllAre(bool[] v, bool level) => v.All(b => b == level);
        static bool IsConstant(bool[] v) => AllAre(v, false) || AllAre(v, true);

        void Add(string text)
        {
            if (!faults.Contains(text))
                faults.Add(text);
        }

        var expected = DriveNets.Select((_, i) => Expected(i)).ToList();

        // Which readers of a net do not read its code in a round, and what they read
        List<InterconnectPin> Failing(int net, int round) =>
            DriveNets[net].Receivers.Where(r => !Vector(r, round).SequenceEqual(expected[net])).ToList();

        var failing = DriveNets.Select((_, i) => Enumerable.Range(0, rounds).Select(r => Failing(i, r)).ToList()).ToList();

        // A net no reader sees in any round is broken as a whole: a short or a stuck net, the same in every round
        bool IsNetFault(int i) => Enumerable.Range(0, rounds).All(r => failing[i][r].Count == DriveNets[i].Receivers.Count);
        bool[] NetVector(int i, int round) => Vector(DriveNets[i].Receivers.First(), round);

        var reported = new HashSet<int>();
        for (var i = 0; i < DriveNets.Count; i++)
        {
            if (reported.Contains(i) || !IsNetFault(i))
                continue;

            reported.Add(i);
            var v = NetVector(i, 0);

            // Shorted nets read the same combination of their codes (wired AND or OR), several of them often all 0
            var group = Enumerable.Range(0, DriveNets.Count)
                .Where(j => j != i && !reported.Contains(j) && IsNetFault(j) && NetVector(j, 0).SequenceEqual(v))
                .ToList();
            var follows = Enumerable.Range(0, DriveNets.Count).Where(j => j != i && expected[j].SequenceEqual(v)).ToList();

            if (group.Count > 0)
            {
                reported.UnionWith(group);
                var names = string.Join(", ", new[] { i }.Concat(group).Select(j => DriveNets[j].Info.Net));
                Add(IsConstant(v)
                    ? $"Nets {names} all read {(v[0] ? 1 : 0)}: shorted to each other or each to {(v[0] ? "a supply" : "GND")}."
                    : $"Short between nets {names}.");
            }
            else if (IsConstant(v))
            {
                var level = v[0] ? 1 : 0;
                Add($"Net {DriveNets[i].Info.Net} is stuck at {level}: short to {(level == 0 ? "GND" : "a supply")} or to a stronger driver.");
            }
            else if (follows.Count > 0)
            {
                Add($"Net {DriveNets[i].Info.Net} follows net {DriveNets[follows[0]].Info.Net}: short between them.");
            }
            else
            {
                Add($"Net {DriveNets[i].Info.Net}: driven {Bits(expected[i])}, read {Bits(v)}.");
            }
        }

        // The other faults concern single pins; comparing the rounds tells which pin and what is broken
        for (var i = 0; i < DriveNets.Count; i++)
        {
            if (reported.Contains(i))
                continue;

            var net = DriveNets[i];
            var name = net.Info.Net;
            var roundsOf = (InterconnectPin p) => Enumerable.Range(0, rounds).Where(r => net.DriverIn(r) == p).ToList();
            var othersRounds = (InterconnectPin p) => Enumerable.Range(0, rounds).Where(r => net.DriverIn(r) != p).ToList();
            var handled = new HashSet<int>();

            foreach (var pin in net.Info.Pins)
            {
                var own = roundsOf(pin);
                var others = othersRounds(pin);
                var otherReaders = net.Receivers.Where(r => r != pin).ToList();

                // Output defective: nobody sees the net while this pin drives, everything is fine otherwise
                if (own.Count > 0 && others.Count > 0 && otherReaders.Count > 0 &&
                    own.All(r => otherReaders.All(failing[i][r].Contains)) && others.All(r => failing[i][r].Count == 0))
                {
                    Add($"Net {name}: {pin} does not drive the net, the other drivers do: its output is defective.");
                    handled.UnionWith(Enumerable.Range(0, rounds));
                    break;
                }

                if (!net.Receivers.Contains(pin))
                    continue;

                // Only this pin reads wrong while the others drive
                if (others.Count == 0 || !others.All(r => failing[i][r].Count == 1 && failing[i][r][0] == pin))
                    continue;

                if (own.Count > 0 && own.All(r => failing[i][r].Count == 1 && failing[i][r][0] == pin))
                {
                    // The others see what it drives, so it is connected: its input is broken
                    Add($"Net {name}: {pin} does not read the net, the others see it drive: its input is defective.");
                }
                else if (own.Count > 0 && own.All(r => otherReaders.All(failing[i][r].Contains) && !failing[i][r].Contains(pin)))
                {
                    // It sees only itself and the others do not see it: cut off from the net. With two pins either end can be open
                    Add(net.Info.Pins.Count(p => p.CanRead || p.CanDrive) > 2 || otherReaders.Count > 1
                        ? $"Open at {pin} on net {name}: it does not see the other pins, and they do not see it."
                        : $"Open on net {name} between {pin} and {otherReaders[0]}: they do not see each other.");
                }
                else
                {
                    var seen = Vector(pin, others[0]);
                    Add(IsConstant(seen)
                        ? $"Open on net {name}: {pin} does not see the net (reads {(seen[0] ? 1 : 0)}); open at the pin or defective input."
                        : $"{pin} on net {name}: read {Bits(seen)}, expected {Bits(expected[i])}.");
                }

                handled.UnionWith(Enumerable.Range(0, rounds));
                break;
            }

            // Anything else: every reader that missed the driver of a round
            foreach (var round in Enumerable.Range(0, rounds).Where(r => !handled.Contains(r)))
            {
                var driver = net.DriverIn(round);
                foreach (var receiver in failing[i][round])
                {
                    var seen = Vector(receiver, round);
                    var other = Enumerable.Range(0, DriveNets.Count).Where(j => j != i && expected[j].SequenceEqual(seen)).ToList();
                    Add(receiver == driver
                        ? $"Net {name}: {driver} reads {Bits(seen)} while driving {Bits(expected[i])}."
                        : IsConstant(seen)
                            ? $"Open on net {name}: {receiver} does not see {driver} (reads {(seen[0] ? 1 : 0)})."
                            : other.Count > 0
                                ? $"{receiver} reads net {DriveNets[other[0]].Info.Net} instead of net {name}: open and short."
                                : $"{receiver} on net {name}: read {Bits(seen)}, expected {Bits(expected[i])}.");
                }
            }
        }

        foreach (var net in ExpectNets)
        {
            var level = net.Level ? 1 : 0;
            foreach (var reader in net.Readers)
            {
                for (var round = 0; round < Math.Max(1, rounds); round++)
                {
                    bool[] v = rounds == 0 ? [read(reader, 0)] : Vector(reader, round);
                    if (AllAre(v, net.Level))
                        continue;

                    var follows = Enumerable.Range(0, DriveNets.Count).Where(j => expected[j].SequenceEqual(v)).ToList();
                    Add(follows.Count > 0
                        ? $"Short between nets {net.Info.Net} and {DriveNets[follows[0]].Info.Net}: {reader} follows the driven net."
                        : AllAre(v, !net.Level)
                            ? $"Net {net.Info.Net}: {reader} expected {level}, read {1 - level}."
                            : $"Net {net.Info.Net}: {reader} expected {level}, read {Bits(v)}.");
                }
            }
        }

        return faults;
    }
}

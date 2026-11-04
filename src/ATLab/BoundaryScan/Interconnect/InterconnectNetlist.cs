using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.BoundaryScan.Bsdl;
using ATLab.Models;
using ATLab.Pcb.Kicad;

namespace ATLab.BoundaryScan.Interconnect;

/// <summary>A device of the chain with its parsed BSDL file, null if it has none.</summary>
public sealed record InterconnectDevice(BoundaryScanDeviceConfig Config, string Name, BsdlDevice? Bsdl);

/// <summary>A boundary scan pin on a net: the port of a device on a pad of its footprint.</summary>
public sealed record InterconnectPin(InterconnectDevice Device, string Port, string PinName, BsdlCell? Input, BsdlCell? Output)
{
    public string DeviceId => Device.Config.Id;

    public bool CanRead => Input != null;
    public bool CanDrive => Output != null;

    /// <summary>The pin can be kept from driving: it has no driver, or one with an output enable.</summary>
    public bool CanRelease => Output == null || Output.ControlCell != null;

    public bool Is(string deviceId, string port) =>
        DeviceId == deviceId && string.Equals(Port, port, StringComparison.OrdinalIgnoreCase);

    /// <summary>E.g. "U3 pin 12 (PA3)", with the signal name the user gave the port instead of the port.</summary>
    public string DisplayName
    {
        get
        {
            var label = Device.Config.GetPinLabel(Port);
            return $"{Device.Name} pin {PinName} ({(label.Length > 0 ? label : Port)})";
        }
    }

    public override string ToString() => DisplayName;
}

/// <summary>A net of the board with its boundary scan pins and the other pads on it.</summary>
public sealed record InterconnectNetInfo(string Net, IReadOnlyList<InterconnectPin> Pins, IReadOnlyList<string> OtherPads)
{
    public IEnumerable<InterconnectPin> Drivers => Pins.Where(p => p.CanDrive);
    public IEnumerable<InterconnectPin> Readers => Pins.Where(p => p.CanRead);
}

/// <summary>
/// The nets of the board that have boundary scan pins: every device with a BSDL file and a footprint reference, the
/// package pin of each port from the BSDL file, and that pad of the footprint with its net.
/// </summary>
public static class InterconnectNetlist
{
    public static List<InterconnectNetInfo> Build(Board board, IReadOnlyList<InterconnectDevice> devices)
    {
        var pinsByNet = new Dictionary<string, List<InterconnectPin>>(StringComparer.Ordinal);
        var scanPads = new HashSet<string>(StringComparer.Ordinal);

        foreach (var device in devices)
        {
            if (device.Bsdl is not { } bsdl || string.IsNullOrWhiteSpace(device.Config.FootprintReference))
                continue;

            var reference = device.Config.FootprintReference.Trim();
            var footprint = board.Footprints.FirstOrDefault(f => f.Reference == reference);
            if (footprint == null)
                continue;

            var package = !string.IsNullOrWhiteSpace(device.Config.PackageName) && bsdl.Packages.ContainsKey(device.Config.PackageName)
                ? device.Config.PackageName
                : bsdl.DefaultPackage;
            if (!bsdl.Packages.TryGetValue(package, out var packagePins))
                continue;

            foreach (var packagePin in packagePins)
            {
                // The test access port belongs to the chain, power and linkage pins have no cells
                if (bsdl.TapPorts.Contains(packagePin.Port))
                    continue;

                var cells = bsdl.GetPortCells(packagePin.Port);
                if (cells.Input == null && cells.Output == null)
                    continue;

                var pad = footprint.Pads.FirstOrDefault(p => p.Number == packagePin.PinName);
                if (pad is not { HasNet: true })
                    continue;

                var pins = pinsByNet.TryGetValue(pad.Net, out var list) ? list : pinsByNet[pad.Net] = [];
                if (pins.Any(p => p.Is(device.Config.Id, packagePin.Port)))
                    continue;

                pins.Add(new InterconnectPin(device, packagePin.Port, packagePin.PinName, cells.Input, cells.Output));
                scanPads.Add(pad.Key);
            }
        }

        var otherPads = board.Footprints
            .SelectMany(f => f.Pads)
            .Where(p => p.HasNet && pinsByNet.ContainsKey(p.Net) && !scanPads.Contains(p.Key))
            .GroupBy(p => p.Net)
            .ToDictionary(g => g.Key, g => g.Select(p => p.ToString()).ToList());

        return pinsByNet
            .OrderBy(n => n.Key, StringComparer.OrdinalIgnoreCase)
            .Select(n => new InterconnectNetInfo(n.Key, n.Value, otherPads.GetValueOrDefault(n.Key) ?? []))
            .ToList();
    }
}

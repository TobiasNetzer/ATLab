using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.BoundaryScan.Bsdl;
using ATLab.Enums;
using ATLab.Models;

namespace ATLab.BoundaryScan.Buses;

/// <param name="Bsdl">Null when the device has no BSDL file.</param>
public record BusPinDevice(string Name, BsdlDevice? Bsdl);

/// <summary>
/// Checks that every signal of a bus has a pin that can do what the protocol needs.
/// Used by the Boundary Scan tab to show hints and by the test run to reject unusable buses.
/// </summary>
public static class BoundaryScanBusValidator
{
    /// <param name="findDevice">Resolves a device id of the chain, null when the device is not part of it.</param>
    /// <param name="write">Validate for driving the bus, pin groups only need output cells then.</param>
    /// <param name="read">Validate for reading the bus.</param>
    public static List<string> Validate(BoundaryScanBus bus, Func<string, BusPinDevice?> findDevice, bool write = true, bool read = true)
    {
        var problems = new List<string>();

        foreach (var group in bus.Pins.Where(p => p.IsAssigned).GroupBy(p => (p.DeviceId, Port: p.Port.ToUpperInvariant())).Where(g => g.Count() > 1))
            problems.Add($"{string.Join(", ", group.Select(p => p.SignalName))} use the same pin.");

        foreach (var pin in bus.Pins)
        {
            var (needed, output, tristate, input) = Requirements(bus, pin, write, read);

            if (!pin.IsAssigned)
            {
                if (needed)
                    problems.Add($"{pin.SignalName} has no pin assigned.");
                continue;
            }

            var device = findDevice(pin.DeviceId);
            if (device == null)
            {
                problems.Add($"{pin.SignalName}: the device is no longer part of the chain.");
                continue;
            }

            if (device.Bsdl == null)
            {
                problems.Add($"{pin.SignalName}: {device.Name} has no BSDL file.");
                continue;
            }

            if (device.Bsdl.TapPorts.Contains(pin.Port))
            {
                problems.Add($"{pin.SignalName}: {pin.Port} is a JTAG pin.");
                continue;
            }

            var cells = device.Bsdl.GetPortCells(pin.Port);

            if (output && cells.Output == null)
                problems.Add($"{pin.SignalName}: {device.Name} {pin.Port} cannot be driven.");
            // Only a pin with a driver has to release the line, an input-only pin is never driven
            else if (tristate && cells.Output != null && cells.Output.ControlCell == null)
                problems.Add($"{pin.SignalName}: {device.Name} {pin.Port} cannot be switched to Z.");

            if (input && cells.Input == null)
                problems.Add($"{pin.SignalName}: {device.Name} {pin.Port} cannot be read.");
        }

        if (bus.Type == BoundaryScanBusType.SPI &&
            bus.GetPin(BoundaryScanBusSignal.MOSI)?.IsAssigned != true &&
            bus.GetPin(BoundaryScanBusSignal.MISO)?.IsAssigned != true)
        {
            problems.Add("SPI needs at least MOSI or MISO.");
        }

        return problems;
    }

    private static (bool Needed, bool Output, bool Tristate, bool Input) Requirements(BoundaryScanBus bus, BoundaryScanBusPin pin, bool write, bool read) =>
        pin.Signal switch
        {
            // Open drain: a 1 releases the line, ACKs, read data and clock stretching are read back
            BoundaryScanBusSignal.SCL or BoundaryScanBusSignal.SDA => (true, true, true, true),
            BoundaryScanBusSignal.SCK => (true, true, false, false),
            BoundaryScanBusSignal.MOSI or BoundaryScanBusSignal.CS => (false, true, false, false),
            BoundaryScanBusSignal.MISO => (false, false, false, true),
            _ => (true, write, read, read)
        };
}
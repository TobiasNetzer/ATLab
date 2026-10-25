using System.Collections.Generic;
using System.Linq;
using ATLab.BoundaryScan.Bsdl;
using ATLab.Models;

namespace ATLab.BoundaryScan;

/// <param name="Name">Name shown in messages, e.g. the device name or the chip type.</param>
/// <param name="Bsdl">Null when the device has no BSDL file.</param>
public record ConfiguredChainDevice(string Name, BoundaryScanDeviceConfig Config, BsdlDevice? Bsdl);

/// <summary>
/// Compares a detected chain with the configured one. Used by the Boundary Scan tab and by test runs,
/// so both accept and reject the same boards.
/// </summary>
public static class BoundaryScanChainMatcher
{
    /// <summary>
    /// The BSDL pattern masks the bits marked X, usually the version, so other silicon revisions match too.
    /// Without BSDL file the stored IDCODE has to match exactly, a device without stored IDCODE matches any chip.
    /// </summary>
    public static bool IsSameDevice(BoundaryScanDeviceConfig config, BsdlDevice? bsdl, uint? idCode)
    {
        if (bsdl?.IdCodePattern != null)
            return idCode is { } id && bsdl.MatchesIdCode(id);

        return string.IsNullOrEmpty(config.IdCode) || config.IdCode == (idCode?.ToString("X8") ?? string.Empty);
    }

    /// <summary>Returns every difference between the configured and the detected chain, empty when they match.</summary>
    public static List<string> Compare(IReadOnlyList<ConfiguredChainDevice> devices, JtagChainInfo chain)
    {
        var problems = new List<string>();

        if (chain.Taps.Count != devices.Count)
            problems.Add($"Found {chain.Taps.Count} device(s), the project expects {devices.Count}.");

        for (var i = 0; i < devices.Count && i < chain.Taps.Count; i++)
        {
            var device = devices[i];
            var found = chain.Taps[i].IdCode;

            if (!IsSameDevice(device.Config, device.Bsdl, found))
            {
                var expected = device.Bsdl?.IdCodePattern != null
                    ? $"the BSDL file of {device.Bsdl.EntityName}"
                    : $"0x{device.Config.IdCode}";

                problems.Add($"Device {i + 1} ({device.Name}): IDCODE {Format(found)} does not match {expected}.");
            }
        }

        var unknown = devices.Select((d, i) => (Device: d, Position: i + 1)).Where(d => d.Device.Config.IrLength <= 0).ToList();

        foreach (var (device, position) in unknown)
            problems.Add($"Device {position} ({device.Name}): instruction length unknown. Assign a BSDL file or enter it.");

        if (unknown.Count == 0 && chain.Taps.Count == devices.Count)
        {
            var configured = devices.Sum(d => d.Config.IrLength);
            if (configured != chain.TotalIrLength)
                problems.Add($"Instruction register is {chain.TotalIrLength} bits, the devices add up to {configured} bits.");
        }

        return problems;
    }

    private static string Format(uint? idCode) => idCode is { } id ? $"0x{id:X8}" : "none";
}

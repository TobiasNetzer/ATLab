using System;
using System.Globalization;

namespace ATLab.Models;

/// <summary>
/// Writes and reads the test hardware device ID as it is stored in project files: hexadecimal, e.g. "0xA101".
/// </summary>
public static class TestHardwareIdFormat
{
    public static string Format(ushort deviceId) => $"0x{deviceId:X4}";

    /// <summary>Reads an ID that is known to be valid, e.g. of a project file checked by ProjectFileService.</summary>
    public static ushort Parse(string? text) =>
        TryParse(text, out var deviceId)
            ? deviceId
            : throw new FormatException($"Invalid test hardware ID '{text}'.");

    public static bool TryParse(string? text, out ushort deviceId)
    {
        deviceId = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var hex = text.Trim();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];

        return ushort.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out deviceId);
    }

    /// <summary>Name and ID of the active hardware for messages, e.g. "compact Test Interface Adapter (0xA101)".</summary>
    public static string GetDisplayName(string? deviceName, ushort deviceId) =>
        string.IsNullOrWhiteSpace(deviceName) ? Format(deviceId) : $"{deviceName} ({Format(deviceId)})";
}
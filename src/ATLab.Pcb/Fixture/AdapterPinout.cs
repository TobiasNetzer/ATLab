using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ATLab.Pcb.Fixture;

/// <summary>What an adapter pin is connected to on the Test Interface Adapter.</summary>
public enum ResourceKind
{
    Meas,
    Stim,
    ExtStim,
    Sense,
    MeasBus,
    ProgPassThrough,
    Interface,
    Unknown
}

/// <summary>One pin of an adapter connector, as listed in the pinout file.</summary>
public sealed class AdapterPin
{
    public string Pin { get; init; } = string.Empty;
    public string Signal { get; init; } = string.Empty;
    public ResourceKind Kind { get; init; }
    public int? Channel { get; init; }

    /// <summary>"In" or "Out" of a stimulation relay contact.</summary>
    public string? Terminal { get; init; }

    /// <summary>"H" or "L" of a sense line or an external stimulus.</summary>
    public string? Polarity { get; init; }

    /// <summary>Line of the programming pass-through.</summary>
    public int? Line { get; init; }

    /// <summary>RS485, I2C or UART.</summary>
    public string? Interface { get; init; }

    /// <summary>Signal of the interface, e.g. SDA or VREF.</summary>
    public string? Role { get; init; }

    /// <summary>Connector the pin belongs to, set after loading.</summary>
    [JsonIgnore]
    public AdapterConnector Connector { get; internal set; } = null!;

    /// <summary>Short name of the resource as ATLab calls it, e.g. "MEAS 7" or "I²C SDA".</summary>
    [JsonIgnore]
    public string Resource => Kind switch
    {
        ResourceKind.Meas => $"MEAS {Channel}",
        ResourceKind.Stim => $"STIM {Channel} {Terminal}",
        ResourceKind.ExtStim => $"EXT STIM {Channel} {Polarity}",
        ResourceKind.Sense => $"SENSE {Polarity}",
        ResourceKind.MeasBus => $"DMM {Polarity}",
        ResourceKind.ProgPassThrough => $"PROG {Line}",
        ResourceKind.Interface => $"{InterfaceName} {Role}",
        _ => Signal
    };

    /// <summary>Pin and resource, e.g. "c7 · MEAS 7", for lists.</summary>
    [JsonIgnore]
    public string Display => $"{Pin} · {Resource}";

    [JsonIgnore]
    public string InterfaceName => Interface == "I2C" ? "I²C" : Interface ?? string.Empty;

    /// <summary>Position for sorting: row letter, then number (a1, a2 … b1 …).</summary>
    [JsonIgnore]
    public (string Row, int Number) SortKey
    {
        get
        {
            var digits = Pin.SkipWhile(c => !char.IsDigit(c)).ToArray();
            var row = Pin[..(Pin.Length - digits.Length)];
            return (row, int.TryParse(digits, out var n) ? n : 0);
        }
    }

    public override string ToString() => Display;
}

public sealed class AdapterConnector
{
    public string Reference { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;

    /// <summary>"Fixture" for the DUT connector, "Instruments" or "Programmer" for the others.</summary>
    public string Side { get; init; } = string.Empty;

    public List<AdapterPin> Pins { get; init; } = [];
}

public sealed class AdapterInfo
{
    public string Name { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
    public int MeasChannels { get; init; }
    public int StimChannels { get; init; }
    public int ExtStimChannels { get; init; }
}

/// <summary>
/// Pin assignment of a Test Interface Adapter type, generated from its KiCad board. The pinouts are built into the
/// program (Fixture/*.pinout.json).
/// </summary>
public sealed class AdapterPinout
{
    public const string ExpectedFormatId = "atlab-test-hardware-pinout";

    public string FormatId { get; init; } = string.Empty;
    public int FormatVersion { get; init; }
    public AdapterInfo TestHardware { get; init; } = new();
    public List<AdapterConnector> Connectors { get; init; } = [];

    /// <summary>Connector the fixture is wired to.</summary>
    [JsonIgnore]
    public AdapterConnector DutConnector => Connectors.First(c => c.Side == "Fixture");

    /// <summary>Pins a probe can be wired to, in connector order.</summary>
    [JsonIgnore]
    public IReadOnlyList<AdapterPin> FixturePins => DutConnector.Pins;

    public AdapterPin? FindFixturePin(string? pin) =>
        pin is null ? null : FixturePins.FirstOrDefault(p => string.Equals(p.Pin, pin, StringComparison.OrdinalIgnoreCase));

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static IReadOnlyList<AdapterPinout>? _builtIn;

    /// <summary>Pinouts built into the program, one per adapter type.</summary>
    public static IReadOnlyList<AdapterPinout> BuiltIn => _builtIn ??= LoadBuiltIn();

    public static AdapterPinout Default => BuiltIn[0];

    public static AdapterPinout? Find(string? deviceId) =>
        BuiltIn.FirstOrDefault(p => string.Equals(p.TestHardware.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));

    private static List<AdapterPinout> LoadBuiltIn()
    {
        var assembly = typeof(AdapterPinout).Assembly;
        var list = new List<AdapterPinout>();
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".pinout.json", StringComparison.Ordinal)).Order())
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            list.Add(Parse(stream));
        }

        return list;
    }

    public static AdapterPinout Parse(Stream stream)
    {
        var pinout = JsonSerializer.Deserialize<AdapterPinout>(stream, JsonOptions)
                     ?? throw new InvalidDataException("Empty pinout file.");
        if (pinout.FormatId != ExpectedFormatId)
            throw new InvalidDataException("Not an adapter pinout file.");

        foreach (var connector in pinout.Connectors)
            foreach (var pin in connector.Pins)
                pin.Connector = connector;

        return pinout;
    }
}

using System.Collections.Generic;
using ATLab.Enums;

namespace ATLab.BoundaryScan.Buses;

/// <param name="Signal">Bus signal.</param>
/// <param name="Index">Bit number of a pin group, 0 for all other signals.</param>
public readonly record struct BusLine(BoundaryScanBusSignal Signal, int Index = 0);

/// <summary>
/// Sequence of bus pin states. Every committed state becomes one boundary scan, so it is the resolution
/// of the emulated bus. Lines keep their state until they are set again.
/// </summary>
public sealed class BusWaveform
{
    private readonly Dictionary<BusLine, PinDriveState> _current = new();
    private readonly List<(int State, BusLine Line)> _samples = new();

    public List<IReadOnlyDictionary<BusLine, PinDriveState>> States { get; } = new();

    /// <summary>Lines read back, each one in the given state.</summary>
    public IReadOnlyList<(int State, BusLine Line)> Samples => _samples;

    public void Drive(BusLine line, bool high) => _current[line] = high ? PinDriveState.HIGH : PinDriveState.LOW;

    public void Release(BusLine line) => _current[line] = PinDriveState.HIGH_Z;

    public void Commit() => States.Add(new Dictionary<BusLine, PinDriveState>(_current));

    /// <summary>Reads the line in the last committed state. Returns the index into the sampled values.</summary>
    public int Sample(BusLine line)
    {
        _samples.Add((States.Count - 1, line));
        return _samples.Count - 1;
    }
}

/// <param name="Data">Bytes read from the bus.</param>
/// <param name="Acknowledged">False when an I²C target did not acknowledge, always true for SPI and pin groups.</param>
/// <param name="Messages">Explains a NACK or a bus problem, shown in the step result tooltip.</param>
public record BusTransactionResult(byte[] Data, bool Acknowledged, IReadOnlyList<string> Messages);

/// <summary>A bus transaction encoded as pin states, decoded once the sampled levels are known.</summary>
public interface IBusTransaction
{
    BusWaveform Waveform { get; }

    BusTransactionResult Decode(IReadOnlyList<bool> samples);
}
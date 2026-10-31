using System.Collections.Generic;
using System.Linq;
using ATLab.Enums;

namespace ATLab.BoundaryScan.Buses;

/// <summary>
/// Pin group: the command bytes form a little endian value that is driven onto bit 0..n.
/// A read releases the pins and samples them, the value is returned little endian as well.
/// </summary>
public sealed class PinGroupTransaction : IBusTransaction
{
    private readonly int _readCount;
    private readonly int[] _readSamples = [];

    public BusWaveform Waveform { get; } = new();

    public PinGroupTransaction(int width, byte[] write, int readCount)
    {
        _readCount = readCount;

        if (write.Length > 0)
        {
            for (var bit = 0; bit < width; bit++)
                Waveform.Drive(Line(bit), bit / 8 < write.Length && ((write[bit / 8] >> (bit % 8)) & 1) != 0);

            Waveform.Commit();
        }

        if (readCount > 0)
        {
            for (var bit = 0; bit < width; bit++)
                Waveform.Release(Line(bit));

            Waveform.Commit();
            _readSamples = Enumerable.Range(0, width).Select(bit => Waveform.Sample(Line(bit))).ToArray();
        }
    }

    public BusTransactionResult Decode(IReadOnlyList<bool> samples)
    {
        var data = new byte[_readCount];

        for (var bit = 0; bit < _readSamples.Length && bit / 8 < data.Length; bit++)
        {
            if (samples[_readSamples[bit]])
                data[bit / 8] |= (byte)(1 << (bit % 8));
        }

        return new BusTransactionResult(data, true, []);
    }

    private static BusLine Line(int bit) => new(BoundaryScanBusSignal.BIT, bit);
}
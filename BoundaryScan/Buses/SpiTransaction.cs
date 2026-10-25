using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Enums;

namespace ATLab.BoundaryScan.Buses;

/// <summary>
/// SPI master. Chip select stays active for the whole transaction, the command bytes are sent first and
/// the read bytes are clocked in afterwards while MOSI sends zeros.
/// </summary>
public sealed class SpiTransaction : IBusTransaction
{
    private static readonly BusLine Sck = new(BoundaryScanBusSignal.SCK);
    private static readonly BusLine Mosi = new(BoundaryScanBusSignal.MOSI);
    private static readonly BusLine Miso = new(BoundaryScanBusSignal.MISO);
    private static readonly BusLine Cs = new(BoundaryScanBusSignal.CS);

    // Per read byte the sample index of every bit (index = bit number), -1 without MISO
    private readonly List<int[]> _readBytes = new();

    public BusWaveform Waveform { get; } = new();

    /// <param name="mode">SPI mode 0-3, bit 1 is CPOL, bit 0 is CPHA.</param>
    /// <param name="usedLines">Lines with a pin assigned, MOSI, MISO and CS are optional.</param>
    public SpiTransaction(int mode, bool csActiveHigh, bool lsbFirst, byte[] write, int readCount, IReadOnlySet<BoundaryScanBusSignal> usedLines)
    {
        var idleClock = (mode & 2) != 0;
        var sampleOnSecondEdge = (mode & 1) != 0;
        var hasMosi = usedLines.Contains(BoundaryScanBusSignal.MOSI);
        var hasMiso = usedLines.Contains(BoundaryScanBusSignal.MISO);
        var hasCs = usedLines.Contains(BoundaryScanBusSignal.CS);

        Waveform.Drive(Sck, idleClock);
        if (hasMosi) Waveform.Drive(Mosi, false);
        if (hasMiso) Waveform.Release(Miso);
        if (hasCs) Waveform.Drive(Cs, !csActiveHigh);
        Waveform.Commit();

        if (hasCs)
        {
            Waveform.Drive(Cs, csActiveHigh);
            Waveform.Commit();
        }

        var bytes = write.Concat(Enumerable.Repeat((byte)0, readCount)).ToArray();

        for (var i = 0; i < bytes.Length; i++)
        {
            var samples = new int[8];
            Array.Fill(samples, -1);

            for (var n = 0; n < 8; n++)
            {
                var bit = lsbFirst ? n : 7 - n;
                var mosi = ((bytes[i] >> bit) & 1) != 0;

                if (!sampleOnSecondEdge)
                {
                    // CPHA 0: data is set up before the first edge and sampled on it
                    if (hasMosi) Waveform.Drive(Mosi, mosi);
                    Waveform.Commit();
                    Waveform.Drive(Sck, !idleClock);
                    Waveform.Commit();
                    if (hasMiso) samples[bit] = Waveform.Sample(Miso);
                    Waveform.Drive(Sck, idleClock);
                    Waveform.Commit();
                }
                else
                {
                    // CPHA 1: data changes on the first edge and is sampled on the second
                    Waveform.Drive(Sck, !idleClock);
                    if (hasMosi) Waveform.Drive(Mosi, mosi);
                    Waveform.Commit();
                    Waveform.Drive(Sck, idleClock);
                    Waveform.Commit();
                    if (hasMiso) samples[bit] = Waveform.Sample(Miso);
                }
            }

            if (i >= write.Length)
                _readBytes.Add(samples);
        }

        if (hasCs)
        {
            Waveform.Drive(Cs, !csActiveHigh);
            Waveform.Commit();
        }
    }

    public BusTransactionResult Decode(IReadOnlyList<bool> samples)
    {
        var data = _readBytes
            .Select(bits =>
            {
                var value = 0;
                for (var bit = 0; bit < 8; bit++)
                {
                    if (bits[bit] >= 0 && samples[bits[bit]])
                        value |= 1 << bit;
                }
                return (byte)value;
            })
            .ToArray();

        return new BusTransactionResult(data, true, []);
    }
}

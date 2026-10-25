using System.Collections.Generic;
using System.Linq;
using ATLab.Enums;

namespace ATLab.BoundaryScan.Buses;

/// <summary>
/// I²C master emulated with open-drain pins: a 0 drives the line low, a 1 releases it to the pull-up.
/// A write followed by a read uses a repeated start, like a register read.
/// </summary>
public sealed class I2CTransaction : IBusTransaction
{
    private static readonly BusLine Scl = new(BoundaryScanBusSignal.SCL);
    private static readonly BusLine Sda = new(BoundaryScanBusSignal.SDA);

    private readonly int _address;
    private readonly List<(int Sample, string What)> _acks = new();
    private readonly List<int> _clockChecks = new();
    private readonly List<int[]> _readBytes = new();

    public BusWaveform Waveform { get; } = new();

    public I2CTransaction(int address, byte[] write, int readCount)
    {
        _address = address;

        Waveform.Release(Scl);
        Waveform.Release(Sda);
        Waveform.Commit();

        if (write.Length > 0 || readCount == 0)
        {
            Start();
            WriteByte((byte)(address << 1), $"address 0x{address:X2} (write)");

            for (var i = 0; i < write.Length; i++)
                WriteByte(write[i], $"byte {i + 1} (0x{write[i]:X2})");
        }

        if (readCount > 0)
        {
            if (write.Length > 0)
                RepeatedStart();
            else
                Start();

            WriteByte((byte)((address << 1) | 1), $"address 0x{address:X2} (read)");

            for (var i = 0; i < readCount; i++)
                ReadByte(acknowledge: i < readCount - 1);
        }

        Stop();
    }

    public BusTransactionResult Decode(IReadOnlyList<bool> samples)
    {
        // A released SCL that reads low means the target stretches the clock or the line is shorted.
        // The transaction is precomputed, so waiting for the target is not possible.
        if (_clockChecks.Any(i => !samples[i]))
            return new BusTransactionResult([], false, ["SCL is held low. Clock stretching is not supported, check SCL for a short and the pull-up."]);

        // SDA high at the ACK clock means nobody acknowledged
        var nack = _acks.FirstOrDefault(a => samples[a.Sample]);
        if (nack != default)
            return new BusTransactionResult([], false, [$"No ACK from 0x{_address:X2} for {nack.What}."]);

        var data = _readBytes
            .Select(bits => (byte)bits.Aggregate(0, (value, sample) => (value << 1) | (samples[sample] ? 1 : 0)))
            .ToArray();

        return new BusTransactionResult(data, true, []);
    }

    private void Start()
    {
        Waveform.Release(Sda);
        Waveform.Commit();
        ClockHigh();
        Waveform.Drive(Sda, false);
        Waveform.Commit();
        Waveform.Drive(Scl, false);
        Waveform.Commit();
    }

    private void RepeatedStart()
    {
        Waveform.Release(Sda);
        Waveform.Commit();
        Start();
    }

    private void Stop()
    {
        Waveform.Drive(Sda, false);
        Waveform.Commit();
        ClockHigh();
        Waveform.Release(Sda);
        Waveform.Commit();
    }

    private void ClockHigh()
    {
        Waveform.Release(Scl);
        Waveform.Commit();
        _clockChecks.Add(Waveform.Sample(Scl));
    }

    private void ClockLow()
    {
        Waveform.Drive(Scl, false);
        Waveform.Commit();
    }

    private void SetSda(bool high)
    {
        if (high)
            Waveform.Release(Sda);
        else
            Waveform.Drive(Sda, false);

        Waveform.Commit();
    }

    private void WriteByte(byte value, string what)
    {
        for (var bit = 7; bit >= 0; bit--)
        {
            SetSda(((value >> bit) & 1) != 0);
            ClockHigh();
            ClockLow();
        }

        // The target pulls SDA low during the ninth clock to acknowledge
        SetSda(true);
        ClockHigh();
        _acks.Add((Waveform.Sample(Sda), what));
        ClockLow();
    }

    private void ReadByte(bool acknowledge)
    {
        var bits = new int[8];

        SetSda(true);

        for (var bit = 0; bit < 8; bit++)
        {
            ClockHigh();
            bits[bit] = Waveform.Sample(Sda);
            ClockLow();
        }

        _readBytes.Add(bits);

        // The last byte is not acknowledged, which tells the target to release SDA for the stop condition
        SetSda(!acknowledge);
        ClockHigh();
        ClockLow();
    }
}
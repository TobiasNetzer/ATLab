using System;
using System.Collections;

namespace ATLab.Models;

public class RelayState
{
    private BitArray _relays;

    public int channelCount => _relays.Length;

    public RelayState(int channelCount)
    {
        _relays = new BitArray(channelCount);
    }

    public bool this[int index]
    {
        get => _relays[index];
        set => _relays[index] = value;
    }

    public void TurnOn(int channel)
    {
        if (channel < 0 || channel >= _relays.Length)
            throw new ArgumentOutOfRangeException(nameof(channel));
        _relays[channel] = true;
    }

    public void TurnOff(int channel)
    {
        if (channel < 0 || channel >= _relays.Length)
            throw new ArgumentOutOfRangeException(nameof(channel));
        _relays[channel] = false;
    }

    public void Toggle(int channel)
    {
        if (channel < 0 || channel >= _relays.Length)
            throw new ArgumentOutOfRangeException(nameof(channel));
        _relays[channel] = !_relays[channel];
    }

    public bool IsOn(int channel)
    {
        if (channel < 0 || channel >= _relays.Length)
            throw new ArgumentOutOfRangeException(nameof(channel));
        return _relays[channel];
    }

    public byte[] ToByteArray()
    {
        int byteCount = (_relays.Length + 7) / 8;
        byte[] bytes = new byte[byteCount];
        _relays.CopyTo(bytes, 0);
        return bytes;
    }

    public void FromByteArray(byte[] bytes)
    {
        _relays = new BitArray(bytes) { Length = _relays.Length };
    }
}

using System;
using System.Text;

namespace ATLab.Models;

public class CommandFrame
{
    public ushort Header { get; } = 0xAA55;
    public ushort Command { get; set; } = 0x00;
    public byte ControlByte { get; set; } = 0x00;
    public byte PayloadSize { get; set; } = 0x00;
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    public byte Crc { get; set; } = 0x00;
    
    public const int MinFrameSize = 7;
    public const int MaxPayloadSize = 120;

    public byte[] ToByteArray()
    {
        if (Payload.Length > MaxPayloadSize)
            throw new InvalidOperationException("Payload too large");

        if (Payload.Length != PayloadSize)
            throw new InvalidOperationException("Parameter PayloadSize does not match actual payload length");

        int frameLength = MinFrameSize + Payload.Length;
        byte[] bytes = new byte[frameLength];

        // Header
        bytes[0] = (byte)(Header & 0xFF);
        bytes[1] = (byte)(Header >> 8);

        // Command
        bytes[2] = (byte)(Command & 0xFF);
        bytes[3] = (byte)(Command >> 8);

        // Control + Payload size
        bytes[4] = ControlByte;
        bytes[5] = PayloadSize;

        // Payload
        Array.Copy(Payload, 0, bytes, 6, Payload.Length);

        // CRC (from byte 2 to second-last)
        byte[] crcData = bytes[2..(frameLength - 1)];
        bytes[frameLength - 1] = Crc8.Compute(crcData);

        return bytes;
    }

    public override string ToString()
    {
        string asciiString = Encoding.ASCII.GetString(Payload);
       return $"CMD=0x{Command:X4}, CTRL=0x{ControlByte:X2}, PAYLOAD={asciiString}";
    }
        
}
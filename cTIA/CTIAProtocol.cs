using System;
using System.Text;

namespace ATLab.CTIA;

public class CTIACommandFrame
{
    private ushort Header { get; } = 0xAA55;
    public ushort Command { get; set; } = 0x00;
    private byte ControlByte { get; set; } = 0x00;
    public byte PayloadSize { get; set; } = 0x00;
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    public byte Crc { get; set; } = 0x00;

    private const int MinFrameSize = 7;
    private const int MaxPayloadSize = 120;

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
    
    public static CTIACommandFrame Parse(byte[] data, ushort expectedHeader = 0xAA55)
    {
        if (data == null || data.Length < CTIACommandFrame.MinFrameSize)
            throw new ArgumentException("Data too short to be a valid frame", nameof(data));

        ushort header = (ushort)(data[0] | (data[1] << 8));
        if (header != expectedHeader)
            throw new InvalidOperationException("Invalid frame header");

        byte payloadSize = data[5];
        int expectedLength = 7 + payloadSize;

        if (data.Length != expectedLength)
            throw new InvalidOperationException("Frame length does not match payload size");

        byte[] crcData = new byte[expectedLength - 3];
        Array.Copy(data, 2, crcData, 0, crcData.Length);
        byte calculatedCrc = Crc8.Compute(crcData);
        byte receivedCrc = data[expectedLength - 1];

        if (calculatedCrc != receivedCrc)
            throw new InvalidOperationException("CRC mismatch");

        ushort command = (ushort)(data[2] | (data[3] << 8));
        byte control = data[4];
        byte[] payload = new byte[payloadSize];
        if (payloadSize > 0)
            Array.Copy(data, 6, payload, 0, payloadSize);

        CTIACommandFrame frame = new CTIACommandFrame();
        frame.Command = command;
        frame.ControlByte = control;
        frame.PayloadSize = payloadSize;
        frame.Payload = payload;

        return frame;
    }
        
}
using System;

namespace ATLab.Models;

public class CommandFrameParser
{
    public static CommandFrame Parse(byte[] data, ushort expectedHeader = 0xAA55)
    {
        if (data == null || data.Length < CommandFrame.MinFrameSize)
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

        CommandFrame frame = new CommandFrame();
        frame.Command = command;
        frame.ControlByte = control;
        frame.PayloadSize = payloadSize;
        frame.Payload = payload;

        return frame;
    }

}

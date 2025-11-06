public static class Crc8
{
    private const byte Polynomial = 0x07;

    public static byte Compute(byte[] data, byte init = 0xFF)
    {
        byte crc = init;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x80) != 0)
                    crc = (byte)((crc << 1) ^ Polynomial);
                else
                    crc <<= 1;
            }
        }
        return crc;
    }
}

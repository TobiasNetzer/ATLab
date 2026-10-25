namespace ATLab.Enums;

public enum BoundaryScanBusSignal
{
    SCL,
    SDA,
    SCK,
    MOSI,
    MISO,
    CS,

    /// <summary>Pin of a pin group, the bit is given by the pin index.</summary>
    BIT
}
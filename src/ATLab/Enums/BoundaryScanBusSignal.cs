namespace ATLab.Enums;

/// <summary>Signals of the buses that can be emulated through boundary scan pins.</summary>
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
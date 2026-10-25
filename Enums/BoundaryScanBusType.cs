using System.ComponentModel;

namespace ATLab.Enums;

public enum BoundaryScanBusType
{
    [Description("I²C")]
    I2C,

    [Description("SPI")]
    SPI,

    [Description("Pin Group")]
    PIN_GROUP
}
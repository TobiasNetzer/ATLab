using System.ComponentModel;

namespace ATLab.Enums;

/// <summary>Bus types that can be emulated through boundary scan pins.</summary>
public enum BoundaryScanBusType
{
    [Description("I²C")]
    I2C,

    [Description("SPI")]
    SPI,

    [Description("Pin Group")]
    PIN_GROUP
}
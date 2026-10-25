using System.ComponentModel;

namespace ATLab.Enums;

/// <summary>Interfaces of the test hardware usable by Interface Command steps.</summary>
public enum CommunicationInterfaceType
{
    [Description("I2C")]
    I2C,
    
    [Description("UART")]
    UART,
    
    [Description("RS485")]
    UART_RS485
}
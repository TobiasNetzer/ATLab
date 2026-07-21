using System.ComponentModel;

namespace ATLab.Enums;

public enum CommunicationInterfaceType
{
    [Description("I2C")]
    I2C,
    
    [Description("UART")]
    UART,
    
    [Description("RS485")]
    UART_RS485
}
using System.ComponentModel;

namespace ATLab.Enums;

/// <summary>Clock rate of the I²C interface of the test hardware.</summary>
public enum I2CSpeedMode
{
    I2C_SPEED_UNDEFINED = -1,
    
    [Description("100kHz")]
    I2C_SPEED_100_KHZ = 0,
    
    [Description("400kHz")]
    I2C_SPEED_400_KHZ = 1,
    
    [Description("1MHz")]
    I2C_SPEED_1_MHZ = 2
}
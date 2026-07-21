using System.ComponentModel;

namespace ATLab.Enums;

public enum I2CSpeedMode
{
    [Description("100kHz")]
    I2C_SPEED_100_KHZ,
    
    [Description("400kHz")]
    I2C_SPEED_400_KHZ
}
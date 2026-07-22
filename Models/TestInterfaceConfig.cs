using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class TestInterfaceConfig : ObservableObject
{
    [ObservableProperty]
    private CommunicationInterfaceType _interfaceType = CommunicationInterfaceType.I2C;
    
    [ObservableProperty]
    private I2CSpeedMode _i2CSpeedMode = I2CSpeedMode.I2C_SPEED_100_KHZ;
    
    [ObservableProperty]
    private int _i2CAddress = 0;
    
    [ObservableProperty]
    private SerialTerminationMode _serialTerminationMode = SerialTerminationMode.NONE;
    
    [ObservableProperty]
    private int _baudRate = 9600;
    
    [ObservableProperty]
    private SerialParity _serialParity = SerialParity.NONE;
    
    [ObservableProperty]
    private int _dataBits = 8;
    
    [ObservableProperty]
    private SerialStopBits _stopBits = SerialStopBits.ONE;
    
    [ObservableProperty]
    private int _timeoutMs = 1000;
    
    [ObservableProperty]
    private string _command = string.Empty;
    
    [ObservableProperty]
    private bool _expectResponse;

    public TestInterfaceConfig()
    {
    }
    
    public TestInterfaceConfig(TestInterfaceConfig other)
    {
        InterfaceType = other.InterfaceType;
        I2CSpeedMode = other.I2CSpeedMode;
        I2CAddress = other.I2CAddress;
        SerialTerminationMode = other.SerialTerminationMode;
        BaudRate = other.BaudRate;
        SerialParity = other.SerialParity;
        DataBits = other.DataBits;
        StopBits = other.StopBits;
    }
}
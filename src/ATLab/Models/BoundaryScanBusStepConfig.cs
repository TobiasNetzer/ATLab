using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// Transaction of a "Boundary Scan Bus" test step. Mirrors the Interface source: command bytes are written,
/// then the requested number of bytes is read and evaluated through the response mask.
/// </summary>
public partial class BoundaryScanBusStepConfig : ObservableObject
{
    [ObservableProperty]
    private string _busId = string.Empty;

    /// <summary>7 bit address, only used by I²C buses.</summary>
    [ObservableProperty]
    private int _i2CAddress;

    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private int _bytesToRead;

    public BoundaryScanBusStepConfig()
    {
    }

    public BoundaryScanBusStepConfig(BoundaryScanBusStepConfig other)
    {
        BusId = other.BusId;
        I2CAddress = other.I2CAddress;
        Command = other.Command;
        BytesToRead = other.BytesToRead;
    }
}
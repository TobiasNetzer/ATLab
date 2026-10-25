namespace ATLab.Interfaces;

/// <summary>Holds the active test hardware: the connected Test Interface Adapter or the simulator.</summary>
public interface IHardwareAccessor
{
    ITestHardware Hardware { get; set; }
}
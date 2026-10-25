using System;
using ATLab.Interfaces;

namespace ATLab.Services;

/// <summary>
/// Holds the active test hardware. Set once at start-up to the connected adapter or the simulator.
/// </summary>
public class HardwareAccessor : IHardwareAccessor
{
    public ITestHardware Hardware
    {
        get => field
               ?? throw new InvalidOperationException("Hardware not initialized");

        set;
    }
}
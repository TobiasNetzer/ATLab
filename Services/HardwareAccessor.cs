using System;
using ATLab.Interfaces;

namespace ATLab.Services;

public class HardwareAccessor : IHardwareAccessor
{
    public ITestHardware Hardware
    {
        get => field
               ?? throw new InvalidOperationException("Hardware not initialized");

        set;
    }
}
using ATLab.Interfaces;

namespace ATLab.Services;

public class HardwareAccessor : IHardwareAccessor
{
    public ITestHardware? Hardware { get; set; }
}
using ATLab.Interfaces;

namespace ATLab.Services;

public class SimulationStateService : ISimulationService
{
    public bool IsSimulationMode { get; set; }
}

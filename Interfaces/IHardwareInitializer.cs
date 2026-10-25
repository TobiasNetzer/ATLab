using System.Threading.Tasks;

namespace ATLab.Interfaces;

/// <summary>
/// Connects to the test hardware at start-up, on the port used last time or in simulation mode if configured.
/// </summary>
public interface IHardwareInitializer
{
    Task<bool> InitializeAsync();
}
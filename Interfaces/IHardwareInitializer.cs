using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface IHardwareInitializer
{
    Task<bool> InitializeAsync();
}
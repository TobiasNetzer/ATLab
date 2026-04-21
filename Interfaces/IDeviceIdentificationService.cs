using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IDeviceIdentificationService
{
    Task<string?> GetIdentificationAsync(Device device, CancellationToken token);
}
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>
/// Queries the identification string of a device (e.g. *IDN?) for the test equipment list of the report.
/// </summary>
public interface IDeviceIdentificationService
{
    Task<string?> GetIdentificationAsync(Device device, CancellationToken token);
}
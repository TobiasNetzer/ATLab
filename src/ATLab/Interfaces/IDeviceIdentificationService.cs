using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.Records;

namespace ATLab.Interfaces;

/// <summary>
/// Queries the identification string of a device (e.g. *IDN?) for the test equipment list of the report.
/// </summary>
public interface IDeviceIdentificationService
{
    Task<string?> GetIdentificationAsync(Device device, CancellationToken token);

    /// <summary>Identifies every device marked "Include in Test Report", in project order.</summary>
    Task<IReadOnlyList<DeviceIdentification>> IdentifyReportDevicesAsync(
        IEnumerable<Device> devices,
        CancellationToken token);
}
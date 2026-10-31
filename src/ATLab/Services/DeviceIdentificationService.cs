using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;

namespace ATLab.Services;

/// <summary>Sends a device's identification query for the test equipment section of the report.</summary>
public class DeviceIdentificationService : IDeviceIdentificationService
{
    private readonly ICommandExecutor _executor;

    public DeviceIdentificationService(ICommandExecutor executor)
    {
        _executor = executor;
    }

    public async Task<string?> GetIdentificationAsync(Device device, CancellationToken token)
    {
        if (!device.IsIncludeInReport)
            return null;

        if (string.IsNullOrWhiteSpace(device.IdentificationQuery))
            return null;

        var cmd = new ScriptCommand
        {
            Command = device.IdentificationQuery,
            IsExpectResponse = true,
            IsEvaluate = true,
            TimeoutMs = 2000
        };

        var result = await _executor.ExecuteAsync(cmd, device.Id, token);

        await _executor.ReleaseDeviceAsync();
        
        if (result.Value == null || !result.IsSuccess)
            return null;

        return Encoding.ASCII.GetString(result.Value);
    }

    public async Task<IReadOnlyList<DeviceIdentification>> IdentifyReportDevicesAsync(
        IEnumerable<Device> devices,
        CancellationToken token)
    {
        var result = new List<DeviceIdentification>();

        foreach (var device in devices)
        {
            if (!device.IsIncludeInReport)
                continue;

            var identification = await GetIdentificationAsync(device, token);
            result.Add(new DeviceIdentification(device.Name, identification));
        }

        return result;
    }
}
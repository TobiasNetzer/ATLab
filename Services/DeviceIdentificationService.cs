using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

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
}
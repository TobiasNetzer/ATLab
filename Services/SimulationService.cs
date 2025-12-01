using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class SimulationService : ITestHardwareCommunication
{
    public Task<byte[]> SendAsync(byte[] data, int timeoutMs = 1000)
    {
        return Task.FromResult(data);
    }
    
    public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken);
        return new byte[] { 0x01, 0x02, 0x03, 0x04 };
    }
}
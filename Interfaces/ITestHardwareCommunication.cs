using System.Threading;
using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface ITestHardwareCommunication
{
    Task<byte[]> SendAsync(byte[] data, int timeoutMs = 1000);
    Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default);
}
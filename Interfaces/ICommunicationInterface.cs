using System.Threading;
using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface ICommunicationInterface
{
    Task<byte[]> SendAsync(byte[] data, int timeoutMs = 1000);
    Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default);
}
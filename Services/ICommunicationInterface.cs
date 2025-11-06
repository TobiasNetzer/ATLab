using System;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Services;
public interface ICommunicationInterface
{
    Task<byte[]> SendAsync(byte[] data, int timeoutMs = 1000);
    Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default);
}
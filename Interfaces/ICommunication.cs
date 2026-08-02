using System;
using ATLab.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface ICommunication : IAsyncDisposable
{
    bool IsConnected { get; }
    string Resource { get; }

    Task<OperationResult> ConnectAsync();
    Task<OperationResult> ReconnectAsync();
    Task<OperationResult> DisconnectAsync();

    void SendRaw(byte[] data);
    Task<byte[]> SendAsync(byte[] data, int timeoutMs = 1000);

    Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default);
}

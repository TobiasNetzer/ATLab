using System;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;
namespace ATLab.CTIA;

public interface ICtiaCommunication : IAsyncDisposable
{
    Task<CtiaCommandFrame?> SendCommandAsync(CtiaCommandFrame frame, int timeoutMs = 1000);
    Task<OperationResult> ReconnectAsync();
    Task<CtiaCommandFrame> ReceiveCommandAsync(CancellationToken cancellationToken = default);
}
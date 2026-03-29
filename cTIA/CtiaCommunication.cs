using System;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.CTIA
{
    public class CtiaCommunication: IDisposable
    {
        private readonly ICommunication _communication;
        private readonly IErrorService _errorService;

        public CtiaCommunication(ICommunication communication, IErrorService errorService)
        {
            _communication = communication;
            _errorService = errorService;
        }
        
        public async Task<CtiaCommandFrame?> SendCommandAsync(CtiaCommandFrame frame, int timeoutMs = 1000)
        {
            try
            {
                if (!_communication.IsConnected)
                {
                    var reconnectResult = await _communication.ReconnectAsync();
                    if (!reconnectResult.IsSuccess)
                    {
                        _errorService.AddError("Test hardware is no longer connected. Reconnection attempt failed.");
                        return null;
                    }
                }

                var responseBytes = await _communication.SendAsync(frame.ToByteArray(), timeoutMs);
                return CtiaCommandFrame.Parse(responseBytes);
            }
            catch (TimeoutException)
            {
                _errorService.AddError("Timeout during communication with test hardware.");
                return null;
            }
            catch (Exception ex)
            {
                _errorService.AddError($"Unexpected error while sending command to test hardware: {ex.Message}");
                return null;
            }
        }

        public async Task<OperationResult> ReconnectAsync()
        {
            return await _communication.ReconnectAsync();
        }

        public async Task<CtiaCommandFrame> ReceiveCommandAsync(CancellationToken cancellationToken = default)
        {
            var receivedData = await _communication.ReceiveAsync(cancellationToken);
            return CtiaCommandFrame.Parse(receivedData);
        }

        public void Dispose()
        {
            if (_communication is IDisposable d) d.Dispose();
        }
    }
}
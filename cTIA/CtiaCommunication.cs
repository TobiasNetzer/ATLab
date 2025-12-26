using System;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;

namespace ATLab.CTIA
{
    public class CtiaCommunication: IDisposable
    {
        private readonly ISerialCommunication _serialCommunication;
        public CtiaCommunication(ISerialCommunication serialCommunication)
        {
            _serialCommunication = serialCommunication;
        }
        
        public async Task<CtiaCommandFrame> SendCommandAsync(CtiaCommandFrame frame, int timeoutMs = 1000)
        {
            try
            {
                byte[] responseBytes = await _serialCommunication.SendAsync(frame.ToByteArray(), timeoutMs);
                return CtiaCommandFrame.Parse(responseBytes);
            }
            catch (TimeoutException)
            {
                return new CtiaCommandFrame
                {
                    Command = (ushort)RespCmd.RESP_ERROR,
                    PayloadSize = 1,
                    Payload = [(byte)CTIAStatus.CTIA_TIMEOUT]
                };
            }
            catch (Exception)
            {
                return new CtiaCommandFrame
                {
                    Command = (ushort)RespCmd.RESP_ERROR,
                    PayloadSize = 1,
                    Payload = [(byte)CTIAStatus.CTIA_FAIL]
                };
            }
        }

        public async Task<CtiaCommandFrame> ReceiveCommandAsync(CancellationToken cancellationToken = default)
        {
            byte[] receivedData = await _serialCommunication.ReceiveAsync(cancellationToken);
            return CtiaCommandFrame.Parse(receivedData);
        }

        public void Dispose()
        {
            if (_serialCommunication is IDisposable d) d.Dispose();
        }
    }
}
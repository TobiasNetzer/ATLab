using System;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services
{
    public class CTIACommunication: IDisposable
    {
        private readonly ICommunicationInterface _communicationInterface;
        public CTIACommunication(ICommunicationInterface communicationInterface)
        {
            _communicationInterface = communicationInterface;
        }
        
        public async Task<CTIACommandFrame> SendCommandAsync(CTIACommandFrame frame, int timeoutMs = 1000)
        {
            byte[] responseBytes = await _communicationInterface.SendAsync(frame.ToByteArray(), timeoutMs);
            return CmdFrameParser.Parse(responseBytes);
        }

        public async Task<CTIACommandFrame> ReceiveCommandAsync(CancellationToken cancellationToken = default)
        {
            byte[] receivedData = await _communicationInterface.ReceiveAsync(cancellationToken);
            return CmdFrameParser.Parse(receivedData);
        }

        public void Dispose()
        {
            if (_communicationInterface is IDisposable d) d.Dispose();
        }
    }
}
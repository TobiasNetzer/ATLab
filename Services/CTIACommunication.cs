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
        
        public async Task<CommandFrame> SendCommandAsync(CommandFrame frame, int timeoutMs = 1000)
        {
            byte[] responseBytes = await _communicationInterface.SendAsync(frame.ToByteArray(), timeoutMs);
            return CommandFrameParser.Parse(responseBytes);
        }

        public async Task<CommandFrame> ReceiveCommandAsync(CancellationToken cancellationToken = default)
        {
            byte[] receivedData = await _communicationInterface.ReceiveAsync(cancellationToken);
            return CommandFrameParser.Parse(receivedData);
        }

        public void Dispose()
        {
            if (_communicationInterface is IDisposable d) d.Dispose();
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;

namespace ATLab.CTIA
{
    public class CtiaCommunication: IDisposable
    {
        private readonly ITestHardwareCommunication _testHardwareCommunication;
        public CtiaCommunication(ITestHardwareCommunication testHardwareCommunication)
        {
            _testHardwareCommunication = testHardwareCommunication;
        }
        
        public async Task<CtiaCommandFrame> SendCommandAsync(CtiaCommandFrame frame, int timeoutMs = 1000)
        {
            byte[] responseBytes = await _testHardwareCommunication.SendAsync(frame.ToByteArray(), timeoutMs);
            return CtiaCommandFrame.Parse(responseBytes);
        }

        public async Task<CtiaCommandFrame> ReceiveCommandAsync(CancellationToken cancellationToken = default)
        {
            byte[] receivedData = await _testHardwareCommunication.ReceiveAsync(cancellationToken);
            return CtiaCommandFrame.Parse(receivedData);
        }

        public void Dispose()
        {
            if (_testHardwareCommunication is IDisposable d) d.Dispose();
        }
    }
}
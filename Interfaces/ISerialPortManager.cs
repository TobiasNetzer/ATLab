using ATLab.Models;

namespace ATLab.Interfaces;

public interface ISerialPortManager
{
    ISerialCommunication GetPort(string portName);
    bool IsOpen(string portName);
    void Open(string portName);
    OperationResult TryOpen(string portName);
}

using ATLab.Interfaces;

namespace ATLab.Interfaces;

public interface ISerialPortManager
{
    ISerialCommunication GetPort(string portName);
    bool IsOpen(string portName);
    void Open(string portName);
}

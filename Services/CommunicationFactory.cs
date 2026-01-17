using ATLab.Interfaces;

namespace ATLab.Services;

public class CommunicationFactory : ICommunicationFactory
{
    public ICommunication CreateSerial(string portName, int baudRate = 115200)
        => new SerialPortService(portName, baudRate);
}
